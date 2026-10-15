using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MeuRoguelike;

// Monta a rua (Cenario.cs), a câmera e a interface, e controla as ondas de inimigos.
// Cada sala limpa leva à próxima, com mais inimigos.
public partial class Jogo : Node3D
{
    const float MeiaLargura = Cenario.MeiaLargura;
    const float MeiaFundura = Cenario.MeiaFundura;
    static readonly Vector3 DistanciaCamera = new(0, 15f, 13.5f);   // inclinação de 48°: mostra a sala inteira e as fachadas

    readonly Random sorteio = new();
    readonly List<Vector3> pilares = new();   // centros dos obstáculos da rua
    Jogador jogador;
    Camera3D camera;
    Label textoVida, textoSala, textoMensagem;
    int sala;
    float tremor;
    bool trocandoDeSala;

    public override void _Ready()
    {
        CriarControles();
        pilares.AddRange(Cenario.MontarRua(this));

        jogador = new Jogador();
        AddChild(jogador);
        jogador.VidaMudou += AtualizarVida;
        jogador.Morreu += () => Mensagem("Você morreu\nAperte R para recomeçar");
        jogador.PediuTremor += forca => tremor = Math.Max(tremor, forca);

        camera = new Camera3D { Fov = 40f, RotationDegrees = new Vector3(-48f, 0, 0), Position = DistanciaCamera };
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
        var atraso = argumentos.FirstOrDefault(a => a.StartsWith("--atraso="));
        if (foto != null) _ = TirarFoto(foto["--foto=".Length..], atraso != null ? double.Parse(atraso["--atraso=".Length..], System.Globalization.CultureInfo.InvariantCulture) : 1.5);

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
        // Sala N tem 2 + N inimigos: 1 mago na sala 1 e mais um a cada duas salas; o resto são guerreiros.
        int quantidade = 2 + sala;
        int magos = Math.Min(quantidade - 1, 1 + (sala - 1) / 2);
        for (int i = 0; i < magos; i++)
            CriarInimigo<InimigoMago>(PosicaoLivre());
        for (int i = magos; i < quantidade; i++)
            CriarInimigo<InimigoGuerreiro>(PosicaoLivre());
    }

    public T CriarInimigo<T>(Vector3 posicao) where T : Inimigo, new()
    {
        var inimigo = new T { Alvo = jogador, Position = posicao };
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
    async System.Threading.Tasks.Task TirarFoto(string caminho, double atraso)
    {
        await ToSignal(GetTree().CreateTimer(atraso), SceneTreeTimer.SignalName.Timeout);
        GetViewport().GetTexture().GetImage().SavePng(caminho);
        GD.Print("Foto salva em ", caminho);
        GetTree().Quit();
    }
}
