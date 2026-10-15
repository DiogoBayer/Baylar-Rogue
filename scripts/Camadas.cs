namespace MeuRoguelike;

// Camadas de colisão: dizem quem esbarra em quem.
// O dash tira os inimigos da máscara do jogador, para ele atravessá-los.
public static class Camadas
{
    public const uint Mundo = 1;      // chão, paredes e pilares
    public const uint Jogador = 2;
    public const uint Inimigos = 4;
}
