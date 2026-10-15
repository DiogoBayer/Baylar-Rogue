using System;
using Godot;

namespace MeuRoguelike;

// O personagem controlado pelo jogador: anda, mira com o mouse, ataca e dá dash.
public partial class Jogador : CharacterBody3D
{
    // Números de equilíbrio. Mexer aqui muda a sensação do jogo.
    [Export] public float Velocidade = 7f;
    [Export] public float VelocidadeDash = 24f;
    [Export] public float DuracaoDash = 0.14f;
    [Export] public float RecargaDash = 0.55f;
    [Export] public int VidaMaxima = 5;
    [Export] public int DanoAtaque = 1;
    [Export] public float AlcanceAtaque = 2.3f;
    [Export] public float AnguloAtaque = 65f;     // graus para cada lado da mira
    [Export] public float RecargaAtaque = 0.32f;
    [Export] public float InvencivelAposDano = 0.9f;

    static readonly Color CorCorpo = new(0.3f, 0.75f, 1f);

    public int Vida { get; private set; }
    public bool Morto => Vida <= 0;
    public bool UsarMouse = true;   // os testes automáticos desligam a mira pelo mouse

    public event Action<int> VidaMudou;
    public event Action Morreu;
    public event Action<float> PediuTremor;

    Node3D corpo;       // o boneco: balança e inclina ao andar
    Node3D noMira;      // a setinha e o golpe: só giram para a mira
    StandardMaterial3D materialCorpo;
    MeshInstance3D golpe;
    Vector3 mira = Vector3.Forward;
    Vector3 direcaoDash;
    Vector3 empurrao;
    float tempoDash, tempoRecargaDash, tempoRecargaAtaque, tempoInvencivel;
    float faseAndar;

    public override void _Ready()
    {
        Vida = VidaMaxima;
        MotionMode = MotionModeEnum.Floating;   // visto de cima: sem gravidade
        CollisionLayer = Camadas.Jogador;
        CollisionMask = Camadas.Mundo | Camadas.Inimigos;

        AddChild(new CollisionShape3D { Shape = new CapsuleShape3D { Radius = 0.4f, Height = 1.7f }, Position = new Vector3(0, 0.85f, 0) });

        corpo = new Node3D();
        AddChild(corpo);
        materialCorpo = Visual.MaterialPersonagem(CorCorpo);
        // Luz própria fraca, para o jogador sempre se destacar na rua escura.
        AddChild(new OmniLight3D { LightColor = new Color(0.6f, 0.85f, 1f), LightEnergy = 1.2f, OmniRange = 3.5f, Position = new Vector3(0, 2.2f, 0.4f) });
        corpo.AddChild(Visual.Boneco(1.7f, materialCorpo));

        noMira = new Node3D();
        AddChild(noMira);
        noMira.AddChild(Visual.Seta(new Color(1, 1, 1, 0.6f)));
        golpe = Visual.Leque(AlcanceAtaque, AnguloAtaque, Colors.White);
        noMira.AddChild(golpe);
    }

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;
        tempoRecargaDash -= d;
        tempoRecargaAtaque -= d;
        tempoInvencivel -= d;
        empurrao = empurrao.MoveToward(Vector3.Zero, 45f * d);

        if (Morto)
        {
            Velocity = empurrao;
            MoveAndSlide();
            return;
        }

        var entrada = Input.GetVector("mover_esquerda", "mover_direita", "mover_cima", "mover_baixo");
        var direcao = new Vector3(entrada.X, 0, entrada.Y);

        if (UsarMouse) AtualizarMiraPeloMouse();

        if (Input.IsActionJustPressed("dash") && tempoRecargaDash <= 0)
            ComecarDash(direcao);
        if (Input.IsActionJustPressed("atacar") && tempoRecargaAtaque <= 0)
            Atacar();

        if (tempoDash > 0)
        {
            tempoDash -= d;
            Velocity = direcaoDash * VelocidadeDash;
            if (tempoDash <= 0) CollisionMask = Camadas.Mundo | Camadas.Inimigos;
        }
        else
        {
            Velocity = direcao * Velocidade;
        }
        Velocity += empurrao;
        MoveAndSlide();

        Visual.AnimarAndar(corpo, Velocity, mira, ref faseAndar, d);
        noMira.Rotation = new Vector3(0, Mathf.Atan2(mira.X, mira.Z), 0);
    }

    public override void _Process(double delta)
    {
        // Pisca enquanto está invencível depois de levar dano.
        bool piscando = tempoInvencivel > 0 && tempoDash <= 0 && !Morto;
        corpo.Visible = !piscando || (int)(tempoInvencivel * 16) % 2 == 0;
        noMira.Visible = !Morto;
    }

    void AtualizarMiraPeloMouse()
    {
        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;
        var mouse = GetViewport().GetMousePosition();
        var ponto = new Plane(Vector3.Up, 0).IntersectsRay(camera.ProjectRayOrigin(mouse), camera.ProjectRayNormal(mouse));
        if (ponto is not Vector3 alvo) return;
        var olhar = alvo - GlobalPosition;
        olhar.Y = 0;
        if (olhar.LengthSquared() > 0.04f) mira = olhar.Normalized();
    }

    public void MirarPara(Vector3 direcao)
    {
        direcao.Y = 0;
        if (direcao.LengthSquared() > 0.0001f) mira = direcao.Normalized();
    }

    void ComecarDash(Vector3 direcao)
    {
        direcaoDash = direcao.LengthSquared() > 0.01f ? direcao.Normalized() : mira;
        tempoDash = DuracaoDash;
        tempoRecargaDash = RecargaDash;
        tempoInvencivel = Math.Max(tempoInvencivel, DuracaoDash + 0.05f);
        CollisionMask = Camadas.Mundo;   // atravessa inimigos durante o dash
    }

    // Golpe em leque na direção da mira. Devolve quantos inimigos acertou.
    public int Atacar()
    {
        tempoRecargaAtaque = RecargaAtaque;
        Visual.Piscar(this, golpe, new Color(1, 1, 1, 0.55f));
        int acertos = 0;
        foreach (var no in GetTree().GetNodesInGroup(Inimigo.Grupo))
        {
            if (no is not Inimigo inimigo || inimigo.Morto) continue;
            var para = inimigo.GlobalPosition - GlobalPosition;
            para.Y = 0;
            float distancia = para.Length();
            if (distancia > AlcanceAtaque + inimigo.Raio) continue;
            if (distancia > 0.3f && Mathf.RadToDeg(mira.AngleTo(para)) > AnguloAtaque) continue;
            inimigo.ReceberDano(DanoAtaque, (distancia > 0.01f ? para / distancia : mira) * 6f);
            acertos++;
        }
        // O golpe também destrói as bolas de energia dos magos.
        foreach (var no in GetTree().GetNodesInGroup(Projetil.Grupo))
        {
            if (no is not Projetil bola || bola.Destruido) continue;
            var para = bola.GlobalPosition - GlobalPosition;
            para.Y = 0;
            if (para.Length() > AlcanceAtaque + 0.3f) continue;
            if (para.Length() > 0.3f && Mathf.RadToDeg(mira.AngleTo(para)) > AnguloAtaque) continue;
            bola.Destruir();
            acertos++;
        }
        if (acertos > 0) PediuTremor?.Invoke(0.12f);
        return acertos;
    }

    public void ReceberDano(int dano, Vector3 empurraoRecebido)
    {
        if (Morto || tempoInvencivel > 0) return;
        Vida = Math.Max(0, Vida - dano);
        tempoInvencivel = InvencivelAposDano;
        empurrao = empurraoRecebido;
        Visual.Flash(this, materialCorpo, CorCorpo);
        PediuTremor?.Invoke(0.3f);
        VidaMudou?.Invoke(Vida);
        if (Morto)
        {
            CreateTween().TweenProperty(corpo, "scale", new Vector3(1.3f, 0.15f, 1.3f), 0.25f);
            Morreu?.Invoke();
        }
    }
}
