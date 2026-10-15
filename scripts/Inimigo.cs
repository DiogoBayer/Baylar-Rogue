using System;
using Godot;

namespace MeuRoguelike;

// O que todo inimigo tem em comum: vida, corpo, apanhar, ser empurrado e morrer.
// Cada tipo (Guerreiro, Mago...) só escreve o próprio comportamento em Pensar().
public abstract partial class Inimigo : CharacterBody3D
{
    public const string Grupo = "inimigos";
    protected static readonly Random Sorteio = new();

    [Export] public float Velocidade = 3.2f;
    [Export] public int VidaMaxima = 3;

    public float Raio => 0.45f;
    public int Vida { get; private set; }
    public bool Morto { get; private set; }
    public Jogador Alvo;

    public event Action Morreu;

    protected abstract Color CorCorpo { get; }
    protected virtual float Altura => 1.8f;

    protected Node3D modelo;
    protected StandardMaterial3D materialCorpo;
    protected Vector3 olhar = Vector3.Forward;
    Vector3 empurrao;
    float faseAndar;
    CollisionShape3D colisao;

    public override void _Ready()
    {
        Vida = VidaMaxima;
        AddToGroup(Grupo);
        MotionMode = MotionModeEnum.Floating;
        CollisionLayer = Camadas.Inimigos;
        CollisionMask = Camadas.Mundo | Camadas.Jogador | Camadas.Inimigos;

        colisao = new CollisionShape3D { Shape = new CapsuleShape3D { Radius = Raio, Height = Altura }, Position = new Vector3(0, Altura / 2f, 0) };
        AddChild(colisao);

        modelo = new Node3D();
        AddChild(modelo);
        materialCorpo = Visual.Material(CorCorpo);
        modelo.AddChild(Visual.Boneco(Altura, materialCorpo));
        Montar();
    }

    // Para cada tipo acrescentar o que é só dele (chapéu, linha de mira...).
    protected virtual void Montar() { }

    // Decide o movimento (Velocity) e para onde olhar (olhar) a cada quadro.
    protected abstract void Pensar(float delta, Vector3 direcao, float distancia);

    public override void _PhysicsProcess(double delta)
    {
        if (Morto) return;
        float d = (float)delta;
        empurrao = empurrao.MoveToward(Vector3.Zero, 40f * d);

        if (Alvo == null || Alvo.Morto)
        {
            Velocity = Vector3.Zero;
        }
        else
        {
            var para = Alvo.GlobalPosition - GlobalPosition;
            para.Y = 0;
            float distancia = para.Length();
            Pensar(d, distancia > 0.01f ? para / distancia : olhar, distancia);
        }

        Velocity += empurrao;
        MoveAndSlide();
        Visual.AnimarAndar(modelo, Velocity, olhar, ref faseAndar, d);
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
        AoApanhar();
        Visual.Flash(this, materialCorpo, CorCorpo);
    }

    // Por padrão, apanhar não muda nada; cada tipo pode cancelar o próprio ataque aqui.
    protected virtual void AoApanhar() { }

    void Morrer()
    {
        Morto = true;
        RemoveFromGroup(Grupo);
        AoApanhar();
        colisao.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        materialCorpo.AlbedoColor = Colors.White;
        var tween = CreateTween();
        tween.TweenProperty(modelo, "scale", new Vector3(1.4f, 0.05f, 1.4f), 0.18f);
        tween.TweenCallback(Callable.From(QueueFree));
        Morreu?.Invoke();
    }
}
