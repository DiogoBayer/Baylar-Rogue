using Godot;

namespace MeuRoguelike;

// Mago: fica longe, anda de lado, mostra uma linha de mira e atira uma bola de energia.
// A direção do tiro trava quando a linha aparece: sair da linha (ou dar dash) evita o dano.
public partial class InimigoMago : Inimigo
{
    [Export] public float DistanciaIdeal = 6.5f;
    [Export] public float TempoEntreTiros = 2.2f;
    [Export] public float TempoMira = 0.7f;       // quanto tempo a linha aparece antes do tiro
    [Export] public float AlcanceVisao = 15f;

    static readonly Color CorMira = new(0.85f, 0.55f, 1f);
    protected override Color CorCorpo => new(0.5f, 0.25f, 0.8f);
    protected override float Altura => 1.75f;

    enum Estado { Posicionando, Mirando }
    Estado estado = Estado.Posicionando;
    float tempo;
    float tempoTrocarLado;
    float lado = 1f;
    Vector3 direcaoTiro;
    MeshInstance3D linhaMira;

    public InimigoMago()
    {
        VidaMaxima = 2;
        Velocidade = 2.8f;
    }

    protected override void Montar()
    {
        var chapeu = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0f, BottomRadius = 0.3f, Height = 0.6f },
            MaterialOverride = Visual.Material(new Color(0.25f, 0.1f, 0.45f)),
            Position = new Vector3(0, Altura + 0.15f, 0),
        };
        modelo.AddChild(chapeu);

        linhaMira = Visual.Caixa(new Vector3(0.12f, 0.02f, 10f), Visual.Material(new Color(CorMira, 0.55f), semLuz: true));
        linhaMira.Position = new Vector3(0, 0.06f, 5.4f);
        linhaMira.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        linhaMira.Visible = false;
        modelo.AddChild(linhaMira);

        // Cada mago começa num ritmo diferente, para não atirarem todos juntos.
        tempo = 1f + (float)Sorteio.NextDouble() * 1.2f;
        lado = Sorteio.Next(2) == 0 ? -1f : 1f;
    }

    protected override void Pensar(float delta, Vector3 direcao, float distancia)
    {
        tempo -= delta;
        tempoTrocarLado -= delta;

        if (estado == Estado.Mirando)
        {
            Velocity = Vector3.Zero;
            olhar = direcaoTiro;
            if (tempo <= 0) Atirar();
            return;
        }

        olhar = direcao;
        Vector3 movimento;
        if (distancia < DistanciaIdeal - 1.5f) movimento = -direcao;               // muito perto: recua
        else if (distancia > DistanciaIdeal + 1.5f) movimento = direcao;           // muito longe: chega mais perto
        else movimento = new Vector3(-direcao.Z, 0, direcao.X) * lado * 0.6f;      // na distância certa: anda de lado

        if (tempoTrocarLado <= 0 || IsOnWall())
        {
            lado = -lado;
            tempoTrocarLado = 1.5f + (float)Sorteio.NextDouble() * 1.5f;
        }
        Velocity = movimento * Velocidade;

        if (tempo <= 0 && distancia <= AlcanceVisao)
        {
            estado = Estado.Mirando;
            tempo = TempoMira;
            direcaoTiro = direcao;
            linhaMira.Visible = true;
            materialCorpo.AlbedoColor = CorMira;
        }
    }

    void Atirar()
    {
        var bola = new Projetil
        {
            Direcao = direcaoTiro,
            Position = GlobalPosition + direcaoTiro * 0.7f + new Vector3(0, 1.1f, 0),
        };
        GetParent().AddChild(bola);
        PararDeMirar();
        tempo = TempoEntreTiros + (float)Sorteio.NextDouble() * 0.6f;
    }

    void PararDeMirar()
    {
        estado = Estado.Posicionando;
        linhaMira.Visible = false;
        materialCorpo.AlbedoColor = CorCorpo;
    }

    // Apanhar enquanto mira cancela o tiro.
    protected override void AoApanhar()
    {
        if (estado != Estado.Mirando) return;
        PararDeMirar();
        tempo = 0.9f;
    }
}
