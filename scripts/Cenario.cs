using System;
using System.Collections.Generic;
using Godot;

namespace MeuRoguelike;

// A rua cyberpunk à noite: asfalto molhado, prédios com vitrines e neon, postes, neblina e brilho.
// A área de jogo é a mesma de antes (24 × 16 m); os obstáculos ficam onde ficavam os pilares.
public static class Cenario
{
    public const float MeiaLargura = 12f;   // área de jogo: X de -12 a 12
    public const float MeiaFundura = 8f;    //                Z de -8 a 8

    // Paleta neon da rua. As cores com valores acima de 1 "estouram" e brilham com o glow.
    public static readonly Color Ciano = new(0.1f, 0.9f, 1f);
    public static readonly Color Magenta = new(1f, 0.15f, 0.75f);
    public static readonly Color Ambar = new(1f, 0.6f, 0.1f);
    public static readonly Color Violeta = new(0.6f, 0.3f, 1f);

    // Monta tudo como filhos de "raiz" e devolve o centro de cada obstáculo (para os inimigos não nascerem neles).
    public static List<Vector3> MontarRua(Node3D raiz, int semente = 2077)
    {
        var sorteio = new Random(semente);
        CriarAmbiente(raiz);
        CriarChao(raiz);
        CriarPredios(raiz, sorteio);
        CriarLimites(raiz);
        CriarPostes(raiz);

        var obstaculos = new List<Vector3>
        {
            new(-6, 0, -3), new(6, 0, -3), new(-6, 0, 3.5f), new(6, 0, 3.5f),
        };
        CriarCarro(raiz, obstaculos[0]);
        CriarQuiosque(raiz, obstaculos[1]);
        CriarCacamba(raiz, obstaculos[2]);
        CriarCaixotes(raiz, obstaculos[3], sorteio);
        return obstaculos;
    }

    static void CriarAmbiente(Node3D raiz)
    {
        var ambiente = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.015f, 0.015f, 0.04f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.35f, 0.4f, 0.7f),
            AmbientLightEnergy = 0.35f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
            GlowEnabled = true,
            GlowIntensity = 0.9f,
            GlowStrength = 1.1f,
            GlowBloom = 0.04f,
            GlowHdrThreshold = 0.95f,
            GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Screen,
            SsrEnabled = true,            // reflexos no asfalto molhado
            SsrMaxSteps = 48,
            FogEnabled = true,
            FogLightColor = new Color(0.22f, 0.12f, 0.35f),
            FogDensity = 0.012f,
        };
        raiz.AddChild(new WorldEnvironment { Environment = ambiente });
        // Luar fraco e azulado: só para dar forma e sombra; a cor vem do neon.
        raiz.AddChild(new DirectionalLight3D { ShadowEnabled = true, RotationDegrees = new Vector3(-60f, 30f, 0), LightEnergy = 0.35f, LightColor = new Color(0.55f, 0.65f, 1f) });
    }

    static void CriarChao(Node3D raiz)
    {
        // Asfalto molhado: escuro, liso e um pouco metálico para refletir as luzes.
        var asfalto = new StandardMaterial3D { AlbedoColor = new Color(0.06f, 0.06f, 0.08f), Roughness = 0.12f, Metallic = 0.35f };
        Bloco(raiz, new Vector3(60, 0.2f, 40), new Vector3(0, -0.1f, 4), asfalto, colide: false);
        Bloco(raiz, new Vector3(MeiaLargura * 2 + 2, 0.2f, MeiaFundura * 2 + 2), new Vector3(0, -0.1f, 0), asfalto, colide: true, visivel: false);

        // Calçada em frente aos prédios (só visual, para não virar degrau).
        var concreto = new StandardMaterial3D { AlbedoColor = new Color(0.16f, 0.16f, 0.19f), Roughness = 0.5f };
        Bloco(raiz, new Vector3(60, 0.06f, 2.2f), new Vector3(0, 0.03f, -MeiaFundura + 1.1f), concreto, colide: false);
        Bloco(raiz, new Vector3(60, 0.12f, 0.2f), new Vector3(0, 0.06f, -MeiaFundura + 2.2f), Visual.Material(new Color(0.3f, 0.3f, 0.33f)), colide: false);

        // Faixas da rua: tracejado âmbar fraco no meio.
        var faixa = Neon(Ambar, 0.6f);
        for (float x = -28; x <= 28; x += 3f)
            Bloco(raiz, new Vector3(1.4f, 0.02f, 0.12f), new Vector3(x, 0.011f, 1.5f), faixa, colide: false);
    }

    static void CriarPredios(Node3D raiz, Random sorteio)
    {
        float frente = -MeiaFundura - 0.5f;   // fachada logo atrás da calçada
        var parede = new StandardMaterial3D { AlbedoColor = new Color(0.09f, 0.08f, 0.12f), Roughness = 0.8f };
        Bloco(raiz, new Vector3(60, 9, 1), new Vector3(0, 4.5f, frente - 0.5f), parede, colide: true);

        // Lojas no térreo: vitrine iluminada, porta, toldo e letreiro de neon.
        var lojas = new (string nome, Color cor)[]
        {
            ("RAMEN 24H", Magenta), ("NEURO//LINK", Ciano), ("BAYLAR", Violeta), ("CHROME DOC", Ambar), ("PACHINKO", Magenta), ("NOODLE BAR", Ciano),
        };
        for (int i = 0; i < lojas.Length; i++)
        {
            float x = -22.5f + i * 9f;
            var (nome, cor) = lojas[i];
            var vitrine = Neon(cor.Lerp(Colors.White, 0.55f), 1.4f);
            Bloco(raiz, new Vector3(3.2f, 1.7f, 0.05f), new Vector3(x - 1.2f, 1.25f, frente + 0.03f), vitrine, colide: false);
            Bloco(raiz, new Vector3(1.1f, 2.2f, 0.05f), new Vector3(x + 1.6f, 1.1f, frente + 0.03f), Visual.Material(new Color(0.03f, 0.03f, 0.05f)), colide: false);
            Bloco(raiz, new Vector3(5f, 0.12f, 1.1f), new Vector3(x, 2.35f, frente + 0.55f), Visual.Material(cor.Darkened(0.65f)), colide: false);
            Bloco(raiz, new Vector3(5f, 0.05f, 0.05f), new Vector3(x, 2.3f, frente + 1.1f), Neon(cor, 3f), colide: false);
            Letreiro(raiz, nome, cor, new Vector3(x, 2.95f, frente + 0.06f), tamanho: 100);
            raiz.AddChild(new OmniLight3D { LightColor = cor, LightEnergy = 1.2f, OmniRange = 5f, Position = new Vector3(x, 2.2f, frente + 1.6f) });
        }

        // Andares de cima: janelas acesas ao acaso.
        var janelaAcesa = new[] { Neon(new Color(1f, 0.75f, 0.45f), 0.9f), Neon(new Color(0.5f, 0.8f, 1f), 0.9f), Neon(Magenta, 0.7f) };
        var janelaApagada = Visual.Material(new Color(0.04f, 0.04f, 0.07f));
        for (float x = -27f; x <= 27f; x += 1.8f)
            for (float y = 4.8f; y <= 8.4f; y += 1.5f)
            {
                var material = sorteio.NextDouble() < 0.45 ? janelaAcesa[sorteio.Next(janelaAcesa.Length)] : janelaApagada;
                Bloco(raiz, new Vector3(1f, 0.9f, 0.05f), new Vector3(x, y, frente + 0.03f), material, colide: false);
            }

        // Placa vertical grande, típica do gênero.
        Bloco(raiz, new Vector3(1.2f, 4.5f, 0.3f), new Vector3(8.2f, 5.6f, frente + 0.6f), Visual.Material(new Color(0.05f, 0.05f, 0.08f)), colide: false);
        var placa = Letreiro(raiz, "HOTEL", Ciano, new Vector3(8.2f, 5.6f, frente + 0.8f), tamanho: 130);
        placa.RotationDegrees = new Vector3(0, 0, -90);
        Piscar(placa);
    }

    static void CriarLimites(Node3D raiz)
    {
        // Sul (perto da câmera): mureta baixa de concreto com faixa de neon, para não tapar a visão.
        var concreto = Visual.Material(new Color(0.22f, 0.22f, 0.25f));
        Bloco(raiz, new Vector3(MeiaLargura * 2 + 2, 0.9f, 0.8f), new Vector3(0, 0.45f, MeiaFundura + 0.4f), concreto, colide: true);
        Bloco(raiz, new Vector3(MeiaLargura * 2 + 2, 0.06f, 0.06f), new Vector3(0, 0.7f, MeiaFundura - 0.02f), Neon(Ciano, 2.5f), colide: false);

        // Leste e oeste: barricadas listradas de amarelo e preto.
        foreach (float lado in new[] { -1f, 1f })
        {
            float x = lado * (MeiaLargura + 0.5f);
            Bloco(raiz, new Vector3(1, 1.2f, MeiaFundura * 2), new Vector3(x, 0.6f, 0), Visual.Material(new Color(0.12f, 0.12f, 0.12f)), colide: true);
            for (float z = -MeiaFundura + 0.5f; z < MeiaFundura; z += 1.6f)
                Bloco(raiz, new Vector3(1.02f, 0.3f, 0.8f), new Vector3(x, 0.95f, z), Visual.Material(new Color(0.95f, 0.75f, 0.05f)), colide: false);
            raiz.AddChild(new OmniLight3D { LightColor = Ambar, LightEnergy = 0.8f, OmniRange = 4f, Position = new Vector3(x - lado * 1.2f, 1.5f, 0) });
        }
    }

    static void CriarPostes(Node3D raiz)
    {
        var metal = new StandardMaterial3D { AlbedoColor = new Color(0.15f, 0.15f, 0.18f), Metallic = 0.8f, Roughness = 0.4f };
        float z = -MeiaFundura + 2f;
        Color[] cores = { Ciano, Magenta, Ciano, Magenta };
        float[] posicoes = { -10.5f, -3.5f, 3.5f, 10.5f };
        for (int i = 0; i < posicoes.Length; i++)
        {
            float x = posicoes[i];
            Bloco(raiz, new Vector3(0.14f, 4.2f, 0.14f), new Vector3(x, 2.1f, z), metal, colide: false);
            Bloco(raiz, new Vector3(0.14f, 0.1f, 1.1f), new Vector3(x, 4.15f, z + 0.5f), metal, colide: false);
            Bloco(raiz, new Vector3(0.5f, 0.06f, 0.25f), new Vector3(x, 4.08f, z + 0.95f), Neon(cores[i], 4f), colide: false);
            raiz.AddChild(new OmniLight3D { LightColor = cores[i], LightEnergy = 2.2f, OmniRange = 8f, Position = new Vector3(x, 3.6f, z + 1f) });
        }
    }

    static void CriarCarro(Node3D raiz, Vector3 centro)
    {
        var lataria = new StandardMaterial3D { AlbedoColor = new Color(0.35f, 0.3f, 0.55f), Metallic = 0.6f, Roughness = 0.3f };
        var vidro = new StandardMaterial3D { AlbedoColor = new Color(0.05f, 0.08f, 0.12f), Metallic = 0.5f, Roughness = 0.05f };
        var corpo = Bloco(raiz, new Vector3(3.4f, 1.5f, 1.7f), centro + new Vector3(0, 0.75f, 0), null, colide: true);
        Peca(corpo, new Vector3(3.4f, 0.65f, 1.7f), new Vector3(0, -0.35f, 0), lataria);
        Peca(corpo, new Vector3(1.9f, 0.55f, 1.5f), new Vector3(-0.2f, 0.25f, 0), vidro);
        Peca(corpo, new Vector3(0.05f, 0.14f, 0.4f), new Vector3(1.71f, -0.3f, 0.5f), Neon(Colors.White, 3f));
        Peca(corpo, new Vector3(0.05f, 0.14f, 0.4f), new Vector3(1.71f, -0.3f, -0.5f), Neon(Colors.White, 3f));
        Peca(corpo, new Vector3(0.05f, 0.1f, 1.4f), new Vector3(-1.71f, -0.25f, 0), Neon(new Color(1f, 0.05f, 0.1f), 3f));
        Peca(corpo, new Vector3(3.3f, 0.04f, 0.04f), new Vector3(0, -0.66f, 0.86f), Neon(Magenta, 2.5f));   // neon embaixo do carro
        raiz.AddChild(new OmniLight3D { LightColor = Magenta, LightEnergy = 0.8f, OmniRange = 3f, Position = centro + new Vector3(0, 0.2f, 0) });
    }

    static void CriarQuiosque(Node3D raiz, Vector3 centro)
    {
        var corpo = Bloco(raiz, new Vector3(1.1f, 2.6f, 1.1f), centro + new Vector3(0, 1.3f, 0), Visual.Material(new Color(0.08f, 0.08f, 0.1f)), colide: true);
        Peca(corpo, new Vector3(0.9f, 1.3f, 1.12f), new Vector3(0, 0.2f, 0), Neon(Magenta, 1.6f));
        Peca(corpo, new Vector3(1.12f, 1.3f, 0.9f), new Vector3(0, 0.2f, 0), Neon(Ciano, 1.3f));
        var luz = new OmniLight3D { LightColor = Magenta, LightEnergy = 1.5f, OmniRange = 4.5f, Position = centro + new Vector3(0, 1.6f, 0) };
        raiz.AddChild(luz);
    }

    static void CriarCacamba(Node3D raiz, Vector3 centro)
    {
        var corpo = Bloco(raiz, new Vector3(2.2f, 1.2f, 1.3f), centro + new Vector3(0, 0.6f, 0), new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.22f, 0.15f), Roughness = 0.6f, Metallic = 0.4f }, colide: true);
        Peca(corpo, new Vector3(2.3f, 0.08f, 1.4f), new Vector3(0, 0.62f, 0), Visual.Material(new Color(0.06f, 0.12f, 0.08f)));
        Peca(corpo, new Vector3(0.9f, 0.3f, 0.02f), new Vector3(0, 0.1f, 0.66f), Neon(Ambar, 1.2f));   // adesivo de aviso
    }

    static void CriarCaixotes(Node3D raiz, Vector3 centro, Random sorteio)
    {
        var madeira = Visual.Material(new Color(0.3f, 0.22f, 0.15f));
        var plastico = Visual.Material(new Color(0.1f, 0.3f, 0.45f));
        Bloco(raiz, new Vector3(2f, 1.6f, 1.4f), centro + new Vector3(0, 0.8f, 0), null, colide: true);
        Vector3[] posicoes = { new(-0.5f, 0.4f, -0.3f), new(0.5f, 0.4f, -0.3f), new(-0.5f, 0.4f, 0.35f), new(0.45f, 0.4f, 0.35f), new(0f, 1.2f, 0f) };
        foreach (var p in posicoes)
        {
            var caixa = Visual.Caixa(new Vector3(0.8f, 0.8f, 0.6f), sorteio.Next(2) == 0 ? madeira : plastico);
            caixa.Position = centro + p;
            caixa.RotationDegrees = new Vector3(0, (float)sorteio.NextDouble() * 20f - 10f, 0);
            raiz.AddChild(caixa);
        }
    }

    // Material que emite luz. "forca" acima de 1 faz brilhar com o glow.
    public static StandardMaterial3D Neon(Color cor, float forca)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = cor,
            EmissionEnabled = true,
            Emission = cor,
            EmissionEnergyMultiplier = forca,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
    }

    static Label3D Letreiro(Node3D raiz, string texto, Color cor, Vector3 posicao, int tamanho)
    {
        var letreiro = new Label3D
        {
            Text = texto,
            FontSize = tamanho,
            PixelSize = 0.008f,
            Modulate = new Color(cor.R * 3f, cor.G * 3f, cor.B * 3f),   // acima de 1: brilha
            OutlineSize = 0,
            Position = posicao,
            Shaded = false,
            DoubleSided = false,
        };
        raiz.AddChild(letreiro);
        return letreiro;
    }

    // Letreiro com mau contato: apaga por instantes de vez em quando.
    static void Piscar(Label3D letreiro)
    {
        var aceso = letreiro.Modulate;
        var apagado = new Color(aceso.R * 0.15f, aceso.G * 0.15f, aceso.B * 0.15f);
        var tween = letreiro.CreateTween().SetLoops();
        tween.TweenInterval(2.6);
        tween.TweenProperty(letreiro, "modulate", apagado, 0.04);
        tween.TweenInterval(0.08);
        tween.TweenProperty(letreiro, "modulate", aceso, 0.04);
        tween.TweenInterval(0.12);
        tween.TweenProperty(letreiro, "modulate", apagado, 0.04);
        tween.TweenInterval(0.05);
        tween.TweenProperty(letreiro, "modulate", aceso, 0.04);
    }

    // Caixa (com ou sem colisão). Sem material, só colide, e as peças visuais entram com Peca().
    static Node3D Bloco(Node3D raiz, Vector3 tamanho, Vector3 posicao, StandardMaterial3D material, bool colide, bool visivel = true)
    {
        Node3D no;
        if (colide)
        {
            var corpo = new StaticBody3D { Position = posicao, CollisionLayer = Camadas.Mundo, CollisionMask = 0 };
            corpo.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = tamanho } });
            no = corpo;
        }
        else
        {
            no = new Node3D { Position = posicao };
        }
        if (material != null && visivel) no.AddChild(Visual.Caixa(tamanho, material));
        raiz.AddChild(no);
        return no;
    }

    static void Peca(Node3D pai, Vector3 tamanho, Vector3 posicao, StandardMaterial3D material)
    {
        var peca = Visual.Caixa(tamanho, material);
        peca.Position = posicao;
        pai.AddChild(peca);
    }
}
