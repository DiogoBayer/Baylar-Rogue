using System;
using Godot;

namespace MeuRoguelike;

// Inimigo corpo a corpo: persegue o jogador, avisa que vai atacar (fica amarelo) e então golpeia.
// O aviso é o que dá ao jogador a chance de desviar com o dash, como em Hades.
public partial class Inimigo : CharacterBody3D
{
    public const string Grupo = "inimigos";

    [Export] public float Velocidade = 3.2f;
    [Export] public int VidaMaxima = 3;
    [Export] public float AlcanceAtaque = 1.5f;
    [Export] public float TempoAviso = 0.55f;     // quanto tempo fica amarelo antes de golpear
    [Export] public float TempoRecuperacao = 0.9f;
    [Export] public float AnguloAtaque = 50f;

    static readonly Color CorCorpo = new(0.85f, 0.2f, 0.2f);
    static readonly Color CorAviso = new(1f, 0.85f, 0.1f);

    public float Raio => 0.45f;
    public int Vida { get; private set; }
    public bool Morto { get; private set; }
    public Jogador Alvo;

    public event Action Morreu;

    enum Estado { Perseguindo, Avisando, Recuperando }
    Estado estado = Estado.Perseguindo;
    float tempo;
    Vector3 empurrao;
    Vector3 olhar = Vector3.Forward;
    Node3D corpo;
    StandardMaterial3D materialCorpo;
    MeshInstance3D golpe;
    CollisionShape3D colisao;

    public override void _Ready()
    {
        Vida = VidaMaxima;
        AddToGroup(Grupo);
        MotionMode = MotionModeEnum.Floating;
        CollisionLayer = Camadas.Inimigos;
        CollisionMask = Camadas.Mundo | Camadas.Jogador | Camadas.Inimigos;

        colisao = new CollisionShape3D { Shape = new CapsuleShape3D { Radius = Raio, Height = 1.4f }, Position = new Vector3(0, 0.7f, 0) };
        AddChild(colisao);

        corpo = new Node3D();
        AddChild(corpo);
        materialCorpo = Visual.Material(CorCorpo);
        corpo.AddChild(Visual.Capsula(Raio, 1.4f, materialCorpo));
        var olho = Visual.Caixa(new Vector3(0.4f, 0.12f, 0.1f), Visual.Material(Colors.Black));
        olho.Position = new Vector3(0, 1.05f, 0.42f);
        corpo.AddChild(olho);
        golpe = Visual.Leque(AlcanceAtaque + 0.3f, AnguloAtaque, CorAviso);
        corpo.AddChild(golpe);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Morto) return;
        float d = (float)delta;
        tempo -= d;
        empurrao = empurrao.MoveToward(Vector3.Zero, 40f * d);

        if (Alvo == null || Alvo.Morto)
        {
            Velocity = empurrao;
            MoveAndSlide();
            return;
        }

        var para = Alvo.GlobalPosition - GlobalPosition;
        para.Y = 0;
        float distancia = para.Length();
        var direcao = distancia > 0.01f ? para / distancia : olhar;

        switch (estado)
        {
            case Estado.Perseguindo:
                olhar = direcao;
                Velocity = direcao * Velocidade;
                if (distancia <= AlcanceAtaque)
                {
                    estado = Estado.Avisando;
                    tempo = TempoAviso;
                    materialCorpo.AlbedoColor = CorAviso;
                }
                break;

            case Estado.Avisando:
                Velocity = Vector3.Zero;   // trava a direção: o jogador pode sair da frente
                if (tempo <= 0) Golpear(para, distancia);
                break;

            case Estado.Recuperando:
                olhar = direcao;
                Velocity = direcao * Velocidade * 0.35f;
                if (tempo <= 0) estado = Estado.Perseguindo;
                break;
        }

        Velocity += empurrao;
        MoveAndSlide();
        corpo.Rotation = new Vector3(0, Mathf.Atan2(olhar.X, olhar.Z), 0);
    }

    void Golpear(Vector3 para, float distancia)
    {
        materialCorpo.AlbedoColor = CorCorpo;
        Visual.Piscar(this, golpe, new Color(CorAviso, 0.6f));
        bool naFrente = distancia < 0.3f || Mathf.RadToDeg(olhar.AngleTo(para)) <= AnguloAtaque;
        if (distancia <= AlcanceAtaque + 0.3f && naFrente)
            Alvo.ReceberDano(1, olhar * 12f);
        estado = Estado.Recuperando;
        tempo = TempoRecuperacao;
    }

    public void ReceberDano(int dano, Vector3 empurraoRecebido)
    {
        if (Morto) return;
        Vida -= dano;
        empurrao = empurraoRecebido;
        if (Vida <= 0)
        {
            Morrer();
            return;
        }
        // Apanhar cancela o ataque que estava sendo preparado.
        estado = Estado.Recuperando;
        tempo = 0.35f;
        Visual.Flash(this, materialCorpo, CorCorpo);
    }

    void Morrer()
    {
        Morto = true;
        RemoveFromGroup(Grupo);
        colisao.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        materialCorpo.AlbedoColor = Colors.White;
        var tween = CreateTween();
        tween.TweenProperty(corpo, "scale", new Vector3(1.4f, 0.05f, 1.4f), 0.18f);
        tween.TweenCallback(Callable.From(QueueFree));
        Morreu?.Invoke();
    }
}
