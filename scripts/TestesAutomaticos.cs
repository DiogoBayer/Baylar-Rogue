using System.Threading.Tasks;
using Godot;

namespace MeuRoguelike;

// Testes que rodam sozinhos (godot -- --teste) para conferir que o combate funciona.
public static class TestesAutomaticos
{
    public static async Task Rodar(Jogo jogo, Jogador jogador)
    {
        jogador.UsarMouse = false;
        int falhas = 0;
        void Conferir(bool ok, string descricao)
        {
            GD.Print((ok ? "  OK     " : "  FALHOU ") + descricao);
            if (!ok) falhas++;
        }
        async Task Esperar(double segundos) =>
            await jogo.ToSignal(jogo.GetTree().CreateTimer(segundos), SceneTreeTimer.SignalName.Timeout);

        GD.Print("Testes do combate:");
        await Esperar(0.2);

        // 1. O golpe acerta um inimigo na frente e erra um atrás.
        var naFrente = jogo.CriarInimigo<InimigoGuerreiro>(jogador.GlobalPosition + new Vector3(0, 0, -1.6f));
        var atras = jogo.CriarInimigo<InimigoGuerreiro>(jogador.GlobalPosition + new Vector3(0, 0, 1.6f));
        naFrente.Alvo = atras.Alvo = null;   // parados, só para o teste
        await Esperar(0.1);
        jogador.MirarPara(Vector3.Forward);
        int acertos = jogador.Atacar();
        Conferir(acertos == 1, "golpe acerta só o inimigo da frente");
        Conferir(naFrente.Vida == naFrente.VidaMaxima - 1, "inimigo da frente perdeu 1 de vida");
        Conferir(atras.Vida == atras.VidaMaxima, "inimigo de trás não perdeu vida");

        // 2. Três golpes derrotam o guerreiro.
        for (int i = 0; i < 2; i++)
        {
            await Esperar(0.4);
            jogador.MirarPara(naFrente.GlobalPosition - jogador.GlobalPosition);
            jogador.Atacar();
        }
        Conferir(naFrente.Morto, "guerreiro morre depois de 3 golpes");

        // 3. Um guerreiro perto ataca e tira vida do jogador.
        atras.Alvo = jogador;
        int vidaAntes = jogador.Vida;
        await Esperar(1.6);
        Conferir(jogador.Vida < vidaAntes, "guerreiro perto tira vida do jogador");
        atras.QueueFree();
        await Esperar(1.0);   // passa a invencibilidade depois do dano

        // 4. O mago atira de longe e acerta o jogador parado.
        var mago = jogo.CriarInimigo<InimigoMago>(jogador.GlobalPosition + new Vector3(0, 0, -7f));
        vidaAntes = jogador.Vida;
        bool viuBola = false;
        for (int i = 0; i < 50 && jogador.Vida == vidaAntes; i++)
        {
            await Esperar(0.1);
            viuBola |= jogo.GetTree().GetNodesInGroup(Projetil.Grupo).Count > 0;
        }
        Conferir(viuBola, "mago atira uma bola de energia");
        Conferir(jogador.Vida < vidaAntes, "bola do mago tira vida do jogador");
        Conferir(mago.GlobalPosition.DistanceTo(jogador.GlobalPosition) > 4f, "mago mantém distância");
        mago.Alvo = null;
        await Esperar(1.0);

        // 5. O golpe do jogador destrói uma bola que vem na direção dele.
        var bola = new Projetil { Direcao = Vector3.Back, Position = jogador.GlobalPosition + new Vector3(0, 1.1f, -1.5f), Velocidade = 0.5f };
        jogo.AddChild(bola);
        await Esperar(0.1);
        jogador.MirarPara(Vector3.Forward);
        jogador.Atacar();
        Conferir(bola.Destruido, "golpe destrói a bola de energia");

        // 6. Dois golpes derrotam o mago.
        mago.Position = jogador.GlobalPosition + new Vector3(0, 0, -1.5f);
        for (int i = 0; i < 2; i++)
        {
            await Esperar(0.4);
            jogador.MirarPara(mago.GlobalPosition - jogador.GlobalPosition);
            jogador.Atacar();
        }
        Conferir(mago.Morto, "mago morre depois de 2 golpes");

        GD.Print(falhas == 0 ? "TESTES OK" : $"TESTES FALHARAM: {falhas}");
        jogo.GetTree().Quit(falhas == 0 ? 0 : 1);
    }
}
