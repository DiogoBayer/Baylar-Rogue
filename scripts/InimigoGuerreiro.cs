using Godot;

namespace MeuRoguelike;

// Guerreiro corpo a corpo: persegue o jogador, avisa que vai atacar (fica amarelo) e então golpeia.
// O aviso é o que dá ao jogador a chance de desviar com o dash, como em Hades.
public partial class InimigoGuerreiro : Inimigo
{
    [Export] public float AlcanceAtaque = 1.5f;
    [Export] public float TempoAviso = 0.55f;     // quanto tempo fica amarelo antes de golpear
    [Export] public float TempoRecuperacao = 0.9f;
    [Export] public float AnguloAtaque = 50f;

    static readonly Color CorAviso = new(1f, 0.85f, 0.1f);
    protected override Color CorCorpo => new(0.85f, 0.2f, 0.2f);

    enum Estado { Perseguindo, Avisando, Recuperando }
    Estado estado = Estado.Perseguindo;
    float tempo;
    MeshInstance3D golpe;

    protected override void Montar()
    {
        golpe = Visual.Leque(AlcanceAtaque + 0.3f, AnguloAtaque, CorAviso);
        modelo.AddChild(golpe);
    }

    protected override void Pensar(float delta, Vector3 direcao, float distancia)
    {
        tempo -= delta;
        switch (estado)
        {
            case Estado.Perseguindo:
                olhar = direcao;
                Velocity = direcao * Velocidade;
                if (distancia <= AlcanceAtaque)
                {
                    estado = Estado.Avisando;
                    tempo = TempoAviso;
                    PintarCorpo(CorAviso);
                }
                break;

            case Estado.Avisando:
                Velocity = Vector3.Zero;   // trava a direção: o jogador pode sair da frente
                if (tempo <= 0) Golpear(direcao, distancia);
                break;

            case Estado.Recuperando:
                olhar = direcao;
                Velocity = direcao * Velocidade * 0.35f;
                if (tempo <= 0) estado = Estado.Perseguindo;
                break;
        }
    }

    void Golpear(Vector3 direcao, float distancia)
    {
        PintarCorpo(CorCorpo);
        Visual.Piscar(this, golpe, new Color(CorAviso, 0.6f));
        bool naFrente = distancia < 0.3f || Mathf.RadToDeg(olhar.AngleTo(direcao)) <= AnguloAtaque;
        if (distancia <= AlcanceAtaque + 0.3f && naFrente)
            Alvo.ReceberDano(1, olhar * 12f);
        estado = Estado.Recuperando;
        tempo = TempoRecuperacao;
    }

    // Apanhar cancela o golpe que estava sendo preparado.
    protected override void AoApanhar()
    {
        PintarCorpo(CorCorpo);
        estado = Estado.Recuperando;
        tempo = 0.35f;
    }
}
