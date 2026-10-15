# Meu Roguelike — contexto do projeto

Jogo roguelike 2.5D de ação em tempo real do Diogo. **O Diogo não programa**: ele traz as ideias e decide; o Claude escreve todo o código, testa e explica. Escreva sempre em português do Brasil.

## Como trabalhar com o Diogo

- Explique em linguagem simples o que mudou e como testar. Nada de jargão sem explicar.
- Seja um parceiro crítico: aponte riscos de escopo e de diversão, mas sem travar com perguntas.
- Cresça em passos pequenos e jogáveis. A cada passo: compilar, rodar os testes, tirar uma foto, e só então entregar.
- Referências de estudo (não copiar código, arte nem som): Cult of the Lamb decompilado em `C:\Users\diogo\estudo-cult-of-the-lamb\src` e scripts Lua do Hades em `C:\Program Files (x86)\Steam\steamapps\common\Hades\Content\Scripts`.

## Ferramentas

- Godot 4.7.2 .NET (C#), instalado pelo winget em `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_*`. O `godot` não está no PATH; use o caminho completo (veja `Jogar.bat`).
- .NET SDK 8. Compilar: `dotnet build`.
- Primeira vez num clone novo: `godot_console --headless --import --path .`.

## Como testar

- Testes automáticos do combate: `<godot_console> --headless --path . -- --teste` → imprime `TESTES OK` e sai com código 0.
- Foto da tela: `<godot_console> --path . -- --foto=C:/.../fotos/x.png` (abre uma janela por ~2 s). A pasta `fotos/` fica fora do Git.
- O Diogo joga com `Jogar.bat` (compila e abre) e abre o editor com `Abrir no Godot.bat`.

## Arquitetura (protótipo 1)

Tudo é montado por código a partir de `cenas/Jogo.tscn`, que só carrega `scripts/Jogo.cs`. Os controles são criados por código em `Jogo.CriarControles`.

| Arquivo | O que faz |
| --- | --- |
| `Jogo.cs` | Sala (chão, paredes, 4 pilares), luz, câmera diagonal que segue o jogador e treme, interface, ondas: sala N tem 2+N inimigos; sala limpa → próxima |
| `Jogador.cs` | Andar (WASD), mirar (mouse no plano do chão), golpe em leque (clique/J), dash (Espaço) com invencibilidade e atravessando inimigos, vida 5, piscar após dano |
| `Inimigo.cs` | Máquina de estados: Perseguindo → Avisando (fica amarelo 0,55 s, parado) → golpe em leque → Recuperando. Apanhar cancela o aviso. 3 de vida |
| `Visual.cs` | Formas e materiais provisórios (cápsulas, caixas, leque do golpe, flash). A arte de verdade entra aqui |
| `Camadas.cs` | Camadas de colisão (mundo, jogador, inimigos) |
| `TestesAutomaticos.cs` | Testes do combate rodados com `-- --teste` |

Decisões já tomadas: C# no Godot (pedido do Diogo); visual 2.5D ainda indefinido (formas provisórias); combate em tempo real; empurrão do golpe do jogador = 6 (com 10 o segundo golpe não alcançava).

## Próximos passos possíveis

Várias salas ligadas por portas (gerador estilo Cult of the Lamb + escolha de porta com recompensa estilo Hades), recompensas/poderes, tipos de inimigo, chefe, progressão entre runs, arte.
