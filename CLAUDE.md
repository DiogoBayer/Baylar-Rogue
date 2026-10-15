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

## Arquitetura

Tudo é montado por código a partir de `cenas/Jogo.tscn`, que só carrega `scripts/Jogo.cs`. Os controles são criados por código em `Jogo.CriarControles`.

| Arquivo | O que faz |
| --- | --- |
| `Jogo.cs` | Sala (chão, paredes, 4 pilares), luz, câmera a 55° que segue o jogador e treme, interface, ondas: sala N tem 2+N inimigos, com 1 mago na sala 1 e mais um a cada duas salas. Sala limpa → próxima. `-- --foto=CAMINHO --atraso=S` salva uma foto |
| `Jogador.cs` | Andar (WASD), mirar (mouse no plano do chão, setinha no chão), golpe em leque (clique/J) que também destrói projéteis, dash (Espaço) com invencibilidade atravessando inimigos, vida 5 |
| `Inimigo.cs` | Base abstrata de todo inimigo: vida, corpo (boneco), empurrão, flash, morte. Cada tipo implementa `Pensar` e pode sobrescrever `Montar`, `AoApanhar`, `CorCorpo`, `Altura` |
| `InimigoGuerreiro.cs` | Corpo a corpo: Perseguindo → Avisando (amarelo 0,55 s, parado) → golpe em leque → Recuperando. 3 de vida. Apanhar cancela o aviso |
| `InimigoMago.cs` | À distância: mantém ~6,5 m (recua, aproxima, anda de lado), Mirando (linha roxa 0,7 s, direção travada) → `Projetil`. 2 de vida, chapéu cônico. Apanhar cancela a mira |
| `Projetil.cs` | Bola de energia (Area3D): linha reta a 9 m/s, some em parede/pilar/jogador ou após 4 s |
| `Visual.cs` | Materiais, `Boneco` (carrega `modelos/boneco.glb`, pinta, ajusta altura e pés em y=0), `AnimarAndar` (balanço e inclinação por código, porque o modelo não tem animações), leque, seta |
| `Camadas.cs` | Camadas de colisão (mundo, jogador, inimigos) |
| `TestesAutomaticos.cs` | 10 testes de combate (guerreiro, mago, projétil) rodados com `-- --teste` |

### Modelo do boneco

- Original do Diogo: `modelos/fonte/BASEmodel.blend` (Blender 2.82, manequim em pose A, esqueleto de 53 ossos, sem animações nem materiais). A pasta tem `.gdignore` para o Godot não importá-la.
- `modelos/boneco.glb` é exportado **sem esqueleto** (mesh estática): com o esqueleto, o osso raiz entrava no Godot girado ~93° e o boneco aparecia deitado; o .blend também tinha 3 modificadores Armature vazios. Exportar com o Blender 5.2 (`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe -b`): remover modificadores Armature, aplicar o Mirror, `parent_clear(CLEAR_KEEP_TRANSFORM)`, apagar Armature/câmera/luz, exportar GLB com `use_selection`.
- Para animar de verdade, será preciso refazer o rig/animações (Blender ou Mixamo) e consertar a rotação do osso raiz.

Decisões já tomadas: C# no Godot (pedido do Diogo); visual 2.5D ainda indefinido; combate em tempo real; empurrão do golpe do jogador = 6 (com 10 o segundo golpe não alcançava); câmera afastada (16,5 de altura) para os magos não ficarem fora da tela.

## Próximos passos possíveis

Várias salas ligadas por portas (gerador estilo Cult of the Lamb + escolha de porta com recompensa estilo Hades), recompensas/poderes, tipos de inimigo, chefe, progressão entre runs, arte.
