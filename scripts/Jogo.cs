using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MeuRoguelike;

// Monta a sala, a câmera e a interface, e controla as ondas de inimigos.
// Cada sala limpa leva à próxima, com mais inimigos.
public partial class Jogo : Node3D
{
    const float MeiaLargura = 12f;   // a sala vai de -12 a 12 em X
    const float MeiaFundura = 8f;    // e de -8 a 8 em Z
    static readonly Vector3 DistanciaCamera = new(0, 13f, 9.1f);

    readonly Random sorteio = new();
    readonly List<Vector3> pilares = new();
    Jogador jogador;
    Camera3D camera;
    Label textoVida, textoSala, textoMensagem;
    int sala;
    float tremor;
    bool trocandoDeSala;

    public override void _Ready()
    {
        CriarControles();
        CriarAmbiente();
        CriarSala();

        jogador = new Jogador();
        AddChild(jogador);
        jogador.VidaMudou += AtualizarVida;
        jogador.Morreu += () => Mensagem("Você morreu\nAperte R para recomeçar");
        jogador.PediuTremor += forca => tremor = Math.Max(tremor, forca);

        camera = new Camera3D { Fov = 40f, RotationDegrees = new Vector3(-55f, 0, 0), Position = DistanciaCamera };
        AddChild(camera);
        CriarInterface();
        AtualizarVida(jogador.Vida);

        var argumentos = OS.GetCmdlineUserArgs();
        if (argumentos.Contains("--teste"))
        {
            _ = TestesAutomaticos.Rodar(this, jogador);
            return;
        }
        var foto = argumentos.FirstOrDefault(a => a.StartsWith("--foto="));
        if (foto != null) _ = TirarFoto(foto["--foto=".Length..]);

        ProximaSala();
    }

    public override void _Process(double delta)
    {
        float d = (float)delta;
        if (Input.IsActionJustPressed("reiniciar")) GetTree().ReloadCurrentScene();

        // Câmera segue o jogador com suavidade e treme quando há impacto.
        var destino = jogador.GlobalPosition + DistanciaCamera;
        var posicao = camera.Position.Lerp(destino, 1f - Mathf.Exp(-8f * d));
        tremor = Math.Max(0, tremor - d);
        if (tremor > 0)
            posicao += new Vector3((float)sorteio.NextDouble() - 0.5f, 0, (float)sorteio.NextDouble() - 0.5f) * tremor * 1.2f;
        camera.Position = posicao;

        if (!trocandoDeSala && !jogador.Morto && sala > 0 && GetTree().GetNodesInGroup(Inimigo.Grupo).Count == 0)
            _ = SalaLimpa();
    }

    async System.Threading.Tasks.Task SalaLimpa()
    {
        trocandoDeSala = true;
        Mensagem("Sala limpa!");
        await ToSignal(GetTree().CreateTimer(1.8), SceneTreeTimer.SignalName.Timeout);
        if (jogador.Morto) return;
        Mensagem("");
        ProximaSala();
        trocandoDeSala = false;
    }

    void ProximaSala()
    {
        sala++;
        textoSala.Text = $"Sala {sala}";
        int quantidade = 2 + sala;
        for (int i = 0; i < quantidade; i++)
            CriarInimigo(PosicaoLivre());
    }

    public Inimigo CriarInimigo(Vector3 posicao)
    {
        var inimigo = new Inimigo { Alvo = jogador, Position = posicao };
        // A partir da sala 3, os inimigos ficam um pouco mais rápidos.
        inimigo.Velocidade += Math.Max(0, sala - 2) * 0.25f;
        AddChild(inimigo);
        return inimigo;
    }

    // Um lugar dentro da sala, longe do jogador e fora dos pilares.
    Vector3 PosicaoLivre()
    {
        for (int tentativa = 0; tentativa < 100; tentativa++)
        {
            var p = new Vector3(
                (float)(sorteio.NextDouble() * 2 - 1) * (MeiaLargura - 1.5f), 0,
                (float)(sorteio.NextDouble() * 2 - 1) * (MeiaFundura - 1.5f));
            if (p.DistanceTo(jogador.GlobalPosition) < 6f) continue;
            if (pilares.Any(pilar => pilar.DistanceTo(p) < 1.8f)) continue;
            return p;
        }
        return new Vector3(MeiaLargura - 2, 0, MeiaFundura - 2);
    }

    static void CriarControles()
    {
        Tecla("mover_cima", Key.W, Key.Up);
        Tecla("mover_baixo", Key.S, Key.Down);
        Tecla("mover_esquerda", Key.A, Key.Left);
        Tecla("mover_direita", Key.D, Key.Right);
        Tecla("dash", Key.Space, Key.Shift);
        Tecla("atacar", Key.J);
        Tecla("reiniciar", Key.R);
        InputMap.ActionAddEvent("atacar", new InputEventMouseButton { ButtonIndex = MouseButton.Left });
    }

    static void Tecla(string acao, params Key[] teclas)
    {
        if (InputMap.HasAction(acao)) return;
        InputMap.AddAction(acao);
        foreach (var tecla in teclas)
            InputMap.ActionAddEvent(acao, new InputEventKey { PhysicalKeycode = tecla });
    }

    void CriarAmbiente()
    {
        var ambiente = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.06f, 0.05f, 0.08f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.55f, 0.55f, 0.7f),
            AmbientLightEnergy = 0.6f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
        };
        AddChild(new WorldEnvironment { Environment = ambiente });
        AddChild(new DirectionalLight3D { ShadowEnabled = true, RotationDegrees = new Vector3(-60f, 30f, 0), LightEnergy = 1.1f });
    }

    void CriarSala()
    {
        var chao = Visual.Material(new Color(0.32f, 0.27f, 0.24f));
        var parede = Visual.Material(new Color(0.42f, 0.36f, 0.33f));
        var pedra = Visual.Material(new Color(0.5f, 0.45f, 0.42f));

        Bloco(new Vector3(MeiaLargura * 2 + 2, 0.2f, MeiaFundura * 2 + 2), new Vector3(0, -0.1f, 0), chao);
        Bloco(new Vector3(MeiaLargura * 2 + 2, 1.2f, 1), new Vector3(0, 0.6f, -MeiaFundura - 0.5f), parede);
        Bloco(new Vector3(MeiaLargura * 2 + 2, 1.2f, 1), new Vector3(0, 0.6f, MeiaFundura + 0.5f), parede);
        Bloco(new Vector3(1, 1.2f, MeiaFundura * 2), new Vector3(-MeiaLargura - 0.5f, 0.6f, 0), parede);
        Bloco(new Vector3(1, 1.2f, MeiaFundura * 2), new Vector3(MeiaLargura + 0.5f, 0.6f, 0), parede);

        foreach (var p in new[] { new Vector3(-6, 0, -3), new Vector3(6, 0, -3), new Vector3(-6, 0, 3.5f), new Vector3(6, 0, 3.5f) })
        {
            pilares.Add(p);
            Bloco(new Vector3(1.2f, 2f, 1.2f), p + new Vector3(0, 1f, 0), pedra);
        }
    }

    void Bloco(Vector3 tamanho, Vector3 posicao, StandardMaterial3D material)
    {
        var corpo = new StaticBody3D { Position = posicao, CollisionLayer = Camadas.Mundo, CollisionMask = 0 };
        corpo.AddChild(Visual.Caixa(tamanho, material));
        corpo.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = tamanho } });
        AddChild(corpo);
    }

    void CriarInterface()
    {
        var camada = new CanvasLayer();
        AddChild(camada);
        var estilo = new LabelSettings { FontSize = 26, OutlineSize = 6, OutlineColor = Colors.Black };

        textoVida = new Label { LabelSettings = estilo, Position = new Vector2(20, 14) };
        textoSala = new Label { LabelSettings = estilo, Position = new Vector2(20, 50) };
        textoMensagem = new Label
        {
            LabelSettings = new LabelSettings { FontSize = 48, OutlineSize = 10, OutlineColor = Colors.Black },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        textoMensagem.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var ajuda = new Label
        {
            Text = "WASD: andar   Mouse: mirar   Clique ou J: atacar   Espaço: dash   R: recomeçar",
            LabelSettings = new LabelSettings { FontSize = 18, OutlineSize = 4, OutlineColor = Colors.Black, FontColor = new Color(1, 1, 1, 0.8f) },
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        ajuda.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        ajuda.Position = new Vector2(0, -36);

        camada.AddChild(textoVida);
        camada.AddChild(textoSala);
        camada.AddChild(textoMensagem);
        camada.AddChild(ajuda);
    }

    void AtualizarVida(int vida)
    {
        textoVida.Text = "Vida  " + new string('♥', vida) + new string('·', jogador.VidaMaxima - vida);
    }

    void Mensagem(string texto) => textoMensagem.Text = texto;

    // Usado para conferir o visual sem jogar: salva uma imagem da tela e fecha.
    async System.Threading.Tasks.Task TirarFoto(string caminho)
    {
        await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);
        GetViewport().GetTexture().GetImage().SavePng(caminho);
        GD.Print("Foto salva em ", caminho);
        GetTree().Quit();
    }
}
