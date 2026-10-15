using Godot;

namespace MeuRoguelike;

// Bola de energia do mago. Anda em linha reta, some ao bater em parede, pilar ou no jogador,
// e pode ser destruída pelo golpe do jogador.
public partial class Projetil : Area3D
{
    public const string Grupo = "projeteis";
    const float Raio = 0.25f;

    public Vector3 Direcao = Vector3.Forward;
    public float Velocidade = 9f;
    public int Dano = 1;
    public bool Destruido { get; private set; }

    float tempoDeVida = 4f;

    public override void _Ready()
    {
        AddToGroup(Grupo);
        CollisionLayer = 0;
        CollisionMask = Camadas.Mundo | Camadas.Jogador;
        AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = Raio } });

        var cor = new Color(0.8f, 0.45f, 1f);
        AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = Raio, Height = Raio * 2 }, MaterialOverride = Visual.MaterialBrilhante(cor), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        AddChild(new OmniLight3D { LightColor = cor, LightEnergy = 1.5f, OmniRange = 2.5f });

        BodyEntered += AoTocar;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Destruido) return;
        float d = (float)delta;
        Position += Direcao * Velocidade * d;
        tempoDeVida -= d;
        if (tempoDeVida <= 0) Destruir();
    }

    void AoTocar(Node3D corpo)
    {
        if (Destruido) return;
        if (corpo is Jogador jogador) jogador.ReceberDano(Dano, Direcao * 8f);
        Destruir();
    }

    public void Destruir()
    {
        if (Destruido) return;
        Destruido = true;
        RemoveFromGroup(Grupo);
        SetDeferred(Area3D.PropertyName.Monitoring, false);
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector3.One * 0.01f, 0.12f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
