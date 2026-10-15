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
        var naFrente = jogo.CriarInimigo(jogador.GlobalPosition + new Vector3(0, 0, -1.6f));
        var atras = jogo.CriarInimigo(jogador.GlobalPosition + new Vector3(0, 0, 1.6f));
        naFrente.Alvo = atras.Alvo = null;   // parados, só para o teste
        await Esperar(0.1);
        jogador.MirarPara(Vector3.Forward);
        int acertos = jogador.Atacar();
        Conferir(acertos == 1, "golpe acerta só o inimigo da frente");
        Conferir(naFrente.Vida == naFrente.VidaMaxima - 1, "inimigo da frente perdeu 1 de vida");
        Conferir(atras.Vida == atras.VidaMaxima, "inimigo de trás não perdeu vida");

        // 2. Três golpes derrotam o inimigo.
        for (int i = 0; i < 2; i++)
        {
            await Esperar(0.4);
            jogador.MirarPara(naFrente.GlobalPosition - jogador.GlobalPosition);
            jogador.Atacar();
        }
        Conferir(naFrente.Morto, "inimigo morre depois de 3 golpes");

        // 3. Um inimigo perto ataca e tira vida do jogador.
        atras.Alvo = jogador;
        int vidaAntes = jogador.Vida;
        await Esperar(1.6);
        Conferir(jogador.Vida < vidaAntes, "inimigo perto tira vida do jogador");

        GD.Print(falhas == 0 ? "TESTES OK" : $"TESTES FALHARAM: {falhas}");
        jogo.GetTree().Quit(falhas == 0 ? 0 : 1);
    }
}
