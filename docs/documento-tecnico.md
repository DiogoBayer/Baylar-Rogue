# Baylar Rogue — Documento Técnico e de Design

| | |
| --- | --- |
| **Versão do documento** | 1.0 — 15/10/2026 |
| **Versão do jogo** | `v2-inimigos-mago` |
| **Repositório** | <https://github.com/DiogoBayer/Baylar-Rogue> |
| **Responsável** | Diogo Bayer (ideias e decisões) · código escrito com o Claude |

Este documento reúne **tudo o que define o jogo**: a ideia, as referências que estudamos, as decisões tomadas e como o jogo é construído. Ele deve ser atualizado no mesmo commit de qualquer mudança que altere o que está escrito aqui.

**Como ler as marcações:**

- ✅ **Decidido**: o Diogo já escolheu.
- 💡 **Proposta**: sugestão ainda não confirmada; vira ✅ quando o Diogo aprovar.
- ❓ **Em aberto**: precisa de decisão.

## Sumário

- **Parte I — A ideia do jogo**: 1. Visão · 2. Pilares · 3. Ciclos de jogo · 4. Decisões
- **Parte II — Referências**: 5. Como estudamos · 6. Cult of the Lamb · 7. Hades · 8. Comparação e o que adotamos
- **Parte III — Técnico**: 9. Tecnologia · 10. Como rodar e testar · 11. Estrutura · 12. Arquitetura · 13. Sistemas · 14. Números de equilíbrio · 15. Arte e modelos · 16. Testes · 17. Convenções · 18. Limitações · 19. Roteiro

---

# Parte I — A ideia do jogo

## 1. Visão

Baylar Rogue é um **roguelike de ação em tempo real, em 2.5D**, com câmera de cima na diagonal. O jogador atravessa uma sequência de salas cheias de inimigos, fica mais forte ao longo da run com poderes e recompensas, e enfrenta um chefe. Ao morrer, a run recomeça do zero, mas parte do progresso permanece e abre novas possibilidades.

A sensação desejada é a dos combates de **Hades** e **Cult of the Lamb**: rápidos, legíveis e justos. Cada golpe inimigo é avisado, e a habilidade do jogador (posicionamento, dash, escolha de alvo) decide o resultado.

**Estado atual (protótipo):** uma sala fixa com ondas de dificuldade crescente, jogador com golpe e dash, dois tipos de inimigo (Guerreiro e Mago). Arte, som e interface provisórios.

## 2. Pilares de design

| Pilar | O que significa na prática | Status |
| --- | --- | --- |
| **Combate justo e legível** | Todo ataque inimigo tem aviso visível, e a direção trava no início do aviso. Se o jogador apanhou, ele consegue entender por quê | ✅ (já implementado) |
| **Decisões a cada sala** | O jogador escolhe caminhos e recompensas, e cada escolha muda a run | 💡 |
| **Cada run é diferente** | Mapa, inimigos e poderes variam; combinações de poderes criam estilos de jogo | 💡 |
| **Morrer faz progredir** | Algo permanece entre runs (recursos, desbloqueios), para a derrota não ser só perda | 💡 |
| **Crescer em passos jogáveis** | Cada etapa do desenvolvimento termina com algo jogável e testado | ✅ (forma de trabalho) |

## 3. Ciclos de jogo

### 3.1 Ciclo de combate (segundos)

Ver o aviso do inimigo → posicionar ou dar dash → golpear → repetir. Já existe no protótipo.

### 3.2 Ciclo da run (minutos) 💡

```
Começo da run ─▶ Sala de combate ─▶ Recompensa ─▶ Escolha do próximo caminho ─┐
                      ▲                                                       │
                      └───────────────────────────────────────────────────────┘
          … a cada andar: loja, evento, mini-chefe … no fim: chefe ─▶ próximo andar ou vitória
Morte em qualquer ponto ─▶ volta ao começo
```

### 3.3 Ciclo entre runs (horas) 💡

Recursos guardados após cada run → melhorias permanentes, novas armas, novos poderes no "saco" de recompensas → próxima run com mais opções (não necessariamente mais fácil).

## 4. Decisões

| Tema | Decisão | Status |
| --- | --- | --- |
| Gênero | Roguelike de ação | ✅ |
| Combate | Tempo real | ✅ |
| Perspectiva | 2.5D, câmera de cima a 55° | ✅ (câmera ajustável) |
| Motor e linguagem | Godot 4.7 com C# | ✅ |
| Personagem-base | Manequim do Diogo (`BASEmodel.blend`) | ✅ |
| Estilo visual | Bonecos 2D num mundo 3D **ou** tudo em 3D | ❓ |
| Tema, mundo e história | — | ❓ |
| Significado de "Baylar" | — | ❓ |
| Estrutura do andar | Grade de salas gerada (estilo Cult of the Lamb), sala a sala por portas (estilo Hades), ou mapa de caminhos | ❓ (ver seção 8) |
| Armas | Uma arma fixa ou várias para escolher | ❓ |
| Poderes | Escolher 1 entre 3 opções a cada recompensa, sorteados de um "saco" | 💡 |
| Progressão entre runs | Recurso permanente + árvore de melhorias | 💡 |
| Plataforma | PC (Windows) | ✅ por enquanto |

---

# Parte II — Referências

## 5. Como estudamos

Estudamos dois roguelikes comerciais **para entender as ideias**, não para copiar. O código, a arte e o som deles são protegidos por direitos autorais e **não entram no Baylar Rogue**. Tudo o que adotamos é reescrito do zero, com o nosso código.

| Jogo | Motor | Como lemos | Onde ficou |
| --- | --- | --- | --- |
| Cult of the Lamb | Unity (C#, Mono) | Decompilado com ILSpy (`ilspycmd`) | `C:\Users\diogo\estudo-cult-of-the-lamb\src` (3.574 arquivos, só no computador do Diogo) |
| Hades | Motor próprio (C++) + regras em Lua | Os 98 scripts Lua vêm em texto no jogo | `C:\Program Files (x86)\Steam\steamapps\common\Hades\Content\Scripts` |

O analisador REA não serviu para o Cult of the Lamb (o arquivo de código passa do limite de análise dele) nem foi necessário para o Hades (texto puro).

## 6. Cult of the Lamb

### 6.1 Geração do andar — `MMBiomeGeneration/BiomeGenerator.cs`

O andar é uma **grade de salas** gerada de uma vez, antes de o jogador entrar. A ordem dos passos:

1. **Passeio aleatório** (`CreateRandomWalk`). Começa com uma sala em (0,0). Repete até atingir o número de salas (`NumberOfRooms`, padrão 20): sorteia uma sala existente, sorteia uma direção (N, L, S, O); se o vizinho estiver vazio, cria uma sala ali e liga as duas. O resultado é um formato orgânico, como galhos, sempre todo conectado.
2. **Medição de distâncias** (`FloodFillDistance`). A partir de uma sala, cada vizinha recebe distância +1, como água se espalhando.
3. **Entrada e saída** (`PlaceEntranceAndExit`). Entre as salas que são **becos sem saída** (uma porta só), escolhe o par **mais distante entre si**: uma vira entrada, a outra a sala do chefe ou a saída. O andar sempre parece longo.
4. **Caminho crítico** (`GetCriticalPath`). Da saída, volta até a entrada pegando sempre a vizinha de distância menor. As salas desse trajeto são obrigatórias; as outras são desvios opcionais.
5. **Chave e porta trancada** (`PlaceLockAndKey`). A chave vai para a sala **mais distante fora do caminho crítico**; a porta trancada é sorteada **no caminho crítico, antes do ponto onde sai o desvio para a chave**. Obriga a explorar e nunca deixa o andar impossível.
6. **Salas especiais**: salas fixas, de história e dinâmicas (lojas, eventos) entram depois, em posições que respeitam essas regras.

```
 [E]─[ ]─[ ]─[🔒]─[ ]─[B]      E = entrada, B = chefe
       │                       caminho crítico: E → B
      [ ]─[ ]─[🔑]             desvio: chave no fundo
```

Outros detalhes observados:

- **Semente**: todo sorteio sai de um único gerador `System.Random(Seed)`. A mesma semente gera o mesmo andar, o que permite repetir um andar para testes e criar desafios diários.
- **Salas desenhadas à mão**: o sorteio decide só *onde* fica cada sala e como se ligam. O conteúdo de cada sala vem de um *prefab* pronto (cenário feito pelos artistas).
- **Arma no fim**: a partir do segundo andar, há 65% de chance de uma arma aparecer no fim do andar (exceto em andar de mini-chefe).

### 6.2 Mapa entre andares — pasta `Map/`

Entre andares, o jogador escolhe o caminho num **mapa em camadas** (`MapGenerator`, `MapLayer`), no estilo Slay the Spire:

- Cada **camada** tem um tipo de nó padrão e uma chance (`randomizeNodes`) de trocar por outro tipo.
- Tipos de nó observados (`NodeType`): inimigo comum, inimigo de elite, descanso, tesouro, loja, chefe, mistério, recursos (ouro, madeira, pedra, comida), seguidor, tarô, mini-chefe e eventos especiais (cura, aposta, desafio, escolha de recompensa).
- O gerador **garante um mínimo de recompensas** (`EnsureMinimumRewards`), **distribui armas, tarô e relíquias** (`EvenWeaponTarotRelicRooms`) e remove caminhos cruzados e nós soltos.

### 6.3 Poderes e modificadores da run

Observados por nome de classe, sem estudo detalhado ainda: **cartas de tarô** (bônus passivos escolhidos durante a run), **relíquias** (`CurrentRelic`, habilidade ativa), **maldições** (`CurrentCurse`, ataque secundário), **armas** (`CurrentWeapon`) e **modificadores de dungeon** positivos, negativos e neutros (`DungeonModifier`). 📌 *Estudar a fundo quando chegarmos ao sistema de poderes.*

## 7. Hades

### 7.1 Sala a sala — `RunManager.lua`

Hades **não gera um mapa**. Cada sala é escolhida **na hora em que o jogador passa pela porta** (`ChooseNextRoomData`):

1. Lista todas as salas da região atual (Tártaro, Asfódelo, Elísio…).
2. **Filtra as que podem aparecer** (`IsRoomEligible`): não repete a sala atual, respeita o máximo de aparições por run e por região, respeita requisitos de progresso.
3. **Verifica se alguma é obrigatória agora** (`IsRoomForced`). Se houver, ela vence.
4. Senão, **sorteia** entre as permitidas.

### 7.2 Regras nos dados, não no código — `RoomDataTartarus.lua`

Cada sala é uma ficha com regras. Exemplos reais do Tártaro:

| Sala | Regra |
| --- | --- |
| Mini-chefe (`A_MiniBoss01`) | Entre a sala 5 e a 9 da região; no máximo 1 vez |
| Loja (`A_Shop01`) | Entre a sala 4 e a 7 |
| Antes do chefe (`A_PreBoss01`) | Exatamente na sala 11 |

Entre o mínimo e o máximo, a chance de uma sala ser forçada **cresce a cada sala**: `1 / (máximo − profundidade atual)`. No máximo, vira 100%. Assim a loja "sempre aparece", mas nunca no mesmo lugar.

### 7.3 Recompensas num saco — `ChooseRoomReward` e `RewardStoreData` (`LootData.lua`)

- No começo da run, um **saco** é enchido com as recompensas possíveis, com repetições de propósito (ex.: várias entradas de recurso permanente).
- Cada porta **tira** uma recompensa do saco, e ela **não volta**.
- Se nada no saco serve, o saco é **reabastecido**; depois de 2 reabastecimentos sem sucesso, a recompensa é **cura** (garantia).
- Há sacos diferentes por tipo de progresso (da run, permanente, especiais).
- A recompensa aparece **na porta antes de entrar**, e é isso que dá significado à escolha do caminho.

Efeito: evita sequências de azar (cinco portas seguidas com a mesma coisa) e mantém a variedade.

### 7.4 Raridade dos poderes — `HeroData.lua`, `RoomManager.lua`

Os poderes ("bênçãos" dos deuses) têm raridade sorteada a partir de chances-base guardadas em dados. Para o herói (`BoonData`): raro 10%, épico 5%, lendário 12%. Certas salas sobrescrevem essas chances (`BoonRaritiesOverride`, ex.: raro 90%, épico 25%, lendário 10%), e o cálculo final fica em `GetRarityChances`. 📌 *Estudar como os outros efeitos alteram essas chances quando chegarmos ao sistema de poderes.*

### 7.5 Progressão e dificuldade

Observados por nome, sem estudo detalhado: melhorias permanentes compradas entre runs (`MetaUpgrades`), e o "Pacto de Punição" (`ShrinePoints`), em que o jogador **escolhe deixar o jogo mais difícil** em troca de recompensas. 📌 *Estudar quando chegarmos à progressão entre runs.*

## 8. Comparação e o que adotamos

| Aspecto | Cult of the Lamb | Hades | Baylar Rogue |
| --- | --- | --- | --- |
| Estrutura do andar | Grade inteira gerada antes | Uma sala por vez, escolhida na porta | ❓ Ver propostas abaixo |
| Conteúdo das salas | Prefabs prontos em posições sorteadas | Salas feitas à mão, ordem sorteada | 💡 Salas feitas à mão (ou por regras simples), posições/ordem sorteadas |
| Escolha do jogador | Por onde andar; caminho no mapa entre andares | Qual porta (vendo a recompensa) | 💡 Escolha com recompensa visível |
| Regras de aparição | No código do gerador | Em fichas de dados | 💡 **Fichas de dados** (mais fácil de equilibrar) |
| Recompensas | Garantia de mínimo no mapa | Saco sem reposição + cura de garantia | 💡 **Saco sem reposição** |
| Aleatoriedade | Uma semente para tudo | Vários sorteios sincronizados | 💡 **Semente única** (repetível e testável) |
| Ataques inimigos | Avisados | Avisados | ✅ **Avisados, direção travada** |

**Propostas para a estrutura do andar (❓ decisão do Diogo):**

- **A — Estilo Cult of the Lamb**: grade gerada por passeio aleatório, entrada e chefe nos becos mais distantes, desvios com tesouros e chave. Bom para exploração.
- **B — Estilo Hades**: uma sala de cada vez, duas ou três portas com a recompensa à mostra, regras por profundidade. Bom para ritmo e decisões.
- **C — Híbrido** (sugestão): mapa curto de caminhos entre andares (estilo Cult of the Lamb), salas escolhidas por fichas com regras de profundidade (estilo Hades), e recompensas do saco mostradas antes de entrar.

**Regras que valem para qualquer opção:**

1. **Dados separados do código**: salas, inimigos, poderes e recompensas são fichas; equilibrar é editar fichas.
2. **Dois filtros antes de sortear**: "pode aparecer?" e "tem que aparecer?".
3. **Saco em vez de dado** para recompensas.
4. **Semente única** para toda a geração da run.
5. **Nada copiado**: só as ideias; código, arte, nomes e textos são nossos.

---

# Parte III — Técnico

## 9. Tecnologia

| Item | Escolha | Observação |
| --- | --- | --- |
| Motor | **Godot 4.7.2 .NET** | Usa C# e guarda cenas e configurações em texto, o que permite criar e testar tudo por linha de comando |
| Linguagem | **C# 12 / .NET 8** | SDK 8.0.426. Projeto `MeuRoguelike.csproj` (SDK `Godot.NET.Sdk/4.7.2`) |
| Renderização | Forward+ | MSAA 3D 2x, sombras da luz direcional |
| Física | Godot Physics 3D | `CharacterBody3D` em modo *Floating* (sem gravidade) para personagens; `Area3D` para projéteis |
| Modelagem | **Blender 5.2** | Conversão `.blend` → `.glb` por script (seção 15) |
| Controle de versão | Git + GitHub | Etiquetas `vN-descricao` marcam versões jogáveis |

**Por que não Unity:** também usa C# (é o motor do Cult of the Lamb), mas boa parte do trabalho é feita clicando no editor, e as cenas são difíceis de editar como texto. No Godot, cenas (`.tscn`), projeto (`project.godot`) e importações (`.import`) são texto legível.

## 10. Como rodar, compilar e testar

### Pré-requisitos (Windows)

1. .NET SDK 8: `winget install Microsoft.DotNet.SDK.8`
2. Godot 4.7.2 .NET: `winget install GodotEngine.GodotEngine.Mono`
3. (Só para mexer no modelo) Blender: `winget install BlenderFoundation.Blender`

O winget instala o Godot em `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_*`. Os atalhos `.bat` usam esse caminho; se o Godot estiver em outro lugar, ajuste os dois arquivos.

### Comandos

| Para quê | Como |
| --- | --- |
| Jogar | Dois cliques em `Jogar.bat` (compila e abre) |
| Abrir no editor | Dois cliques em `Abrir no Godot.bat` |
| Compilar | `dotnet build` |
| Primeira importação (clone novo) | `<godot_console> --headless --import --path .` |
| Testes automáticos | `<godot_console> --headless --path . -- --teste` → `TESTES OK` e código de saída 0 |
| Foto da tela | `<godot_console> --path . -- --foto=C:/caminho/foto.png --atraso=2` (abre uma janela, espera N segundos, salva e fecha) |

`<godot_console>` é o `Godot_v4.7.2-stable_mono_win64_console.exe`. Os argumentos depois de `--` são lidos pelo jogo com `OS.GetCmdlineUserArgs()`.

### Controles

| Tecla | Ação |
| --- | --- |
| W A S D / setas | Andar |
| Mouse | Mirar |
| Clique esquerdo / J | Atacar |
| Espaço / Shift | Dash |
| R | Recomeçar |

Os controles são criados por código em `Jogo.CriarControles()`, e não no `project.godot`.

## 11. Estrutura de pastas

```
Baylar-Rogue/
├── project.godot            configuração do projeto Godot
├── MeuRoguelike.csproj      projeto C#
├── Jogar.bat                compila e abre o jogo
├── Abrir no Godot.bat       abre o editor
├── CLAUDE.md                contexto para o assistente de código
├── README.md                página de entrada do repositório
├── cenas/
│   └── Jogo.tscn            cena principal (só carrega Jogo.cs)
├── scripts/                 todo o código C#
├── modelos/
│   ├── boneco.glb           modelo usado no jogo (gerado)
│   └── fonte/               originais do Blender (ignorado pelo Godot)
├── ferramentas/blender/     scripts de conversão de modelos
└── docs/                    este documento
```

Fora do Git (`.gitignore`): `.godot/`, `bin/`, `obj/`, `fotos/`, `*.rar`. Os arquivos `*.uid` e `*.import` **vão** para o Git, porque o Godot precisa deles para manter as referências. As pastas `docs/`, `ferramentas/` e `modelos/fonte/` têm `.gdignore`, para o Godot não importá-las.

## 12. Arquitetura

### 12.1 Visão geral

O jogo é montado **por código**. A cena `Jogo.tscn` tem um único nó com o script `Jogo.cs`, que cria o ambiente, a sala, o jogador, a câmera, a interface e os inimigos. Isso mantém tudo legível em C# e fácil de mudar sem o editor. Quando houver arte e salas desenhadas à mão, partes disso passarão para cenas `.tscn`.

```
Jogo (Node3D)                      ← Jogo.cs: regras da partida
├── WorldEnvironment, DirectionalLight3D
├── StaticBody3D × N               ← chão, paredes, pilares (camada Mundo)
├── Jogador (CharacterBody3D)      ← Jogador.cs
│   ├── corpo (Node3D)             ← boneco; balança ao andar
│   └── noMira (Node3D)            ← seta no chão + leque do golpe; só gira
├── InimigoGuerreiro / InimigoMago (CharacterBody3D)  ← herdam de Inimigo.cs
├── Projetil (Area3D)              ← criado pelo mago
├── Camera3D
└── CanvasLayer                    ← interface (vida, sala, mensagens, ajuda)
```

### 12.2 Scripts

| Arquivo | Responsabilidade |
| --- | --- |
| `Jogo.cs` | Monta a sala; câmera que segue e treme; interface; ondas de inimigos; reinício; modos `--teste` e `--foto` |
| `Jogador.cs` | Movimento, mira, golpe, dash, vida, invencibilidade; eventos `VidaMudou`, `Morreu`, `PediuTremor` |
| `Inimigo.cs` | Classe base abstrata dos inimigos (seção 13.2) |
| `InimigoGuerreiro.cs` | Inimigo corpo a corpo |
| `InimigoMago.cs` | Inimigo à distância |
| `Projetil.cs` | Bola de energia do mago |
| `Visual.cs` | Materiais, carregamento do boneco, animação de andar procedural, leque, seta, flash |
| `Camadas.cs` | Constantes das camadas de colisão |
| `TestesAutomaticos.cs` | Testes de combate de ponta a ponta |

### 12.3 Comunicação entre partes

- **Eventos C#** do jogador (`VidaMudou`, `Morreu`, `PediuTremor`) avisam o `Jogo` para atualizar a interface, mostrar mensagens e tremer a câmera.
- **Grupos do Godot**: `"inimigos"` (`Inimigo.Grupo`) e `"projeteis"` (`Projetil.Grupo`). O golpe do jogador procura alvos nesses grupos, e o `Jogo` conta os inimigos do grupo para saber se a sala foi limpa. Um inimigo sai do grupo **no momento em que morre**, antes da animação de morte acabar.
- **Referência direta**: cada inimigo guarda `Alvo` (o jogador).

### 12.4 Camadas de colisão

| Camada | Valor | Quem | Colide com |
| --- | --- | --- | --- |
| Mundo | 1 | Chão, paredes, pilares | — |
| Jogador | 2 | Jogador | Mundo, Inimigos (durante o dash: só Mundo) |
| Inimigos | 4 | Guerreiro, Mago | Mundo, Jogador, Inimigos |
| — | 0 | Projétil (`Area3D`) | Detecta Mundo e Jogador |

## 13. Sistemas de jogo

### 13.1 Jogador

- **Movimento:** direção do teclado × velocidade, sem aceleração. X é a direita e Z é para baixo na tela.
- **Mira:** raio do mouse a partir da câmera, cruzado com o plano do chão (y = 0). A seta no chão mostra a direção.
- **Golpe:** acerta todo inimigo dentro de um **leque** (raio e ângulo na seção 14) e destrói projéteis no mesmo leque. Empurra os inimigos e faz a câmera tremer quando acerta.
- **Dash:** na direção do movimento (ou da mira, se parado). Deixa invencível e atravessa inimigos.
- **Dano recebido:** perde vida, é empurrado, pisca e fica invencível por um tempo. Com vida 0, a run acaba (R recomeça).

### 13.2 Inimigos (classe base `Inimigo`)

A base cuida do que é comum: vida, corpo, colisão, empurrão, flash ao apanhar, morte (sai do grupo, encolhe e é removido) e animação de andar. Para **criar um novo tipo**, basta herdar de `Inimigo` e implementar:

| Membro | Obrigatório | Para quê |
| --- | --- | --- |
| `Pensar(delta, direcao, distancia)` | Sim | Definir `Velocity` e `olhar` a cada quadro físico. Recebe a direção e a distância até o jogador |
| `CorCorpo` | Sim | Cor do boneco |
| `Altura` | Não | Altura do boneco (padrão 1,8) |
| `Montar()` | Não | Acrescentar peças visuais (chapéu, linha de mira…) |
| `AoApanhar()` | Não | Reagir ao dano, por exemplo cancelando um ataque |

Depois, incluir o tipo na composição das ondas em `Jogo.ProximaSala()`.

**Regra de design (pilar 1):** todo ataque inimigo tem **aviso visível**, e a direção trava no início do aviso.

#### Guerreiro

```
Perseguindo ──(jogador ao alcance)──▶ Avisando (amarelo, parado)
     ▲                                     │ tempo de aviso acabou
     │                                     ▼
Recuperando ◀──────────────────────── Golpe em leque
```

Apanhar durante o aviso cancela o golpe e o leva a *Recuperando*.

#### Mago

```
Posicionando ──(recarga pronta e jogador a até 15 m)──▶ Mirando (linha roxa, parado)
     ▲                                                     │ tempo de mira acabou
     └──────────────────── dispara Projetil ◀──────────────┘
```

- *Posicionando*: recua se o jogador está a menos de `DistanciaIdeal − 1,5`; aproxima-se se está a mais de `DistanciaIdeal + 1,5`; senão anda de lado, trocando de lado a cada 1,5–3 s ou ao bater em parede.
- Cada mago começa com uma recarga sorteada, para não atirarem juntos.
- Apanhar durante a mira cancela o tiro.

### 13.3 Projétil

`Area3D` esférica que anda em linha reta. Some ao tocar o mundo (paredes e pilares bloqueiam tiros), ao tocar o jogador (causando dano e empurrão) ou depois de 4 s. Pode ser destruída pelo golpe do jogador.

### 13.4 Sala e ondas (provisório)

- Sala de 24 × 16 m (X de −12 a 12, Z de −8 a 8), paredes de 1,2 m e pilares de 1,2 × 2 × 1,2 m em (±6, −3) e (±6, 3,5).
- A sala *N* tem **2 + N** inimigos: `min(total − 1, 1 + (N − 1) / 2)` magos e o resto guerreiros.
- Inimigos nascem em posição sorteada a pelo menos 6 m do jogador e 1,8 m dos pilares.
- A partir da sala 3, todos os inimigos ganham +0,25 m/s de velocidade por sala.
- Sala limpa → mensagem por 1,8 s → próxima sala.

Este sistema será substituído pela estrutura de andar escolhida na seção 8.

### 13.5 Câmera

Perspectiva com FOV 40°, inclinada 55° para baixo, a (0; 16,5; 11,55) do jogador. Segue com suavização exponencial (`1 − e^(−8·dt)`) e treme ao acertar (0,12) ou ser atingido (0,3).

## 14. Números de equilíbrio

Ficam no topo de cada classe, a maioria marcada com `[Export]` (editável no inspetor do Godot).

### Jogador (`Jogador.cs`)

| Parâmetro | Valor |
| --- | --- |
| Velocidade | 7 m/s |
| Vida máxima | 5 |
| Dano do golpe | 1 |
| Alcance do golpe | 2,3 m (+ raio do inimigo) |
| Ângulo do golpe | 65° para cada lado |
| Recarga do golpe | 0,32 s |
| Empurrão do golpe | 6 |
| Velocidade do dash | 24 m/s |
| Duração do dash | 0,14 s |
| Recarga do dash | 0,55 s |
| Invencibilidade após dano | 0,9 s |

### Guerreiro (`InimigoGuerreiro.cs`)

| Parâmetro | Valor |
| --- | --- |
| Vida | 3 |
| Velocidade | 3,2 m/s |
| Alcance do golpe | 1,5 m |
| Tempo de aviso | 0,55 s |
| Recuperação | 0,9 s |
| Ângulo do golpe | 50° |
| Dano / empurrão | 1 / 12 |

### Mago (`InimigoMago.cs`)

| Parâmetro | Valor |
| --- | --- |
| Vida | 2 |
| Velocidade | 2,8 m/s |
| Distância ideal | 6,5 m |
| Tempo de mira | 0,7 s |
| Intervalo entre tiros | 2,2 s + até 0,6 s sorteado |
| Primeiro tiro | 1,0 a 2,2 s após nascer |
| Alcance de visão | 15 m |

### Projétil (`Projetil.cs`)

| Parâmetro | Valor |
| --- | --- |
| Velocidade | 9 m/s |
| Dano / empurrão | 1 / 8 |
| Raio | 0,25 m |
| Duração máxima | 4 s |

**Histórico de ajustes:**

- Empurrão do golpe do jogador: 10 → 6, porque o inimigo saía do alcance e o segundo golpe errava.
- Câmera: altura 13 → 16,5, e distância ideal do mago 7,5 → 6,5, porque o mago ficava fora da tela.

## 15. Arte e modelos

- **Original:** `modelos/fonte/BASEmodel.blend` (Blender 2.82). Manequim humano em pose A, cerca de 1.200 vértices, esqueleto de 53 ossos, **sem animações e sem materiais**.
- **No jogo:** `modelos/boneco.glb`, exportado **sem esqueleto** por `ferramentas/blender/exportar_boneco.py`:

  ```
  "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b modelos\fonte\BASEmodel.blend ^
      --python ferramentas\blender\exportar_boneco.py -- modelos\boneco.glb
  ```

  Motivo: com o esqueleto, o osso raiz chegava ao Godot girado cerca de 93° e o boneco aparecia deitado; o `.blend` também tinha três modificadores *Armature* vazios.
- **No código:** `Visual.Boneco(altura, material)` carrega o `.glb`, aplica o material, mede a caixa do modelo e ajusta escala e posição para que os pés fiquem em y = 0 e a altura seja a pedida. A frente do modelo é +Z.
- **Animação:** provisória e procedural (`Visual.AnimarAndar`): pulo, inclinação para a frente e gingado proporcionais à velocidade. Para animações de verdade, o boneco precisa ser animado (Blender ou Mixamo) e o osso raiz, corrigido.
- **Cores provisórias:** jogador azul; guerreiro vermelho (amarelo no aviso); mago roxo (lilás na mira).

## 16. Testes

`TestesAutomaticos.cs` roda dentro do jogo real (sem janela) e verifica:

1. O golpe acerta só o inimigo da frente.
2. O inimigo da frente perde 1 de vida.
3. O inimigo de trás não perde vida.
4. O guerreiro morre com 3 golpes.
5. Um guerreiro perto tira vida do jogador.
6. O mago atira uma bola de energia.
7. A bola do mago tira vida do jogador.
8. O mago mantém distância.
9. O golpe destrói a bola de energia.
10. O mago morre com 2 golpes.

**Regra:** toda mudança de jogabilidade precisa passar nos testes antes do commit, e um sistema novo ganha teste novo.

## 17. Convenções

- **Idioma:** código (classes, métodos, variáveis) e comentários em **português**. Exceções: palavras-chave do C# e a API do Godot.
- **Commits:** mensagem em português, título curto, corpo com o que mudou e por quê.
- **Versões jogáveis:** etiqueta `vN-descricao` (ex.: `v2-inimigos-mago`).
- **Documentação:** mudou um sistema, uma decisão ou um número da seção 14 → atualizar este documento no mesmo commit.
- **Referências:** ideias do Cult of the Lamb e do Hades podem ser citadas aqui; código, arte, nomes e textos deles nunca entram no projeto.
- **Fotos de conferência:** na pasta `fotos/`, fora do Git.

## 18. Limitações conhecidas

- O boneco não tem animações; o balanço por código é provisório.
- Os caminhos do Godot nos `.bat` estão fixos para a instalação via winget.
- Só existe uma sala, sem portas nem mapa.
- Não há som, menu nem salvamento.
- O nome interno do projeto ainda é `MeuRoguelike` (assembly e namespace).

## 19. Roteiro

| Etapa | Conteúdo | Referência principal |
| --- | --- | --- |
| ✅ 1 | Combate básico: jogador, guerreiro, ondas | Hades (avisos) |
| ✅ 2 | Base de inimigos, mago, projétil, boneco | — |
| 3 | Estrutura do andar (decidir A, B ou C da seção 8) e salas ligadas por portas | Cult of the Lamb 6.1–6.2, Hades 7.1–7.2 |
| 4 | Recompensas e poderes, com saco sem reposição e escolha 1 de 3 | Hades 7.3–7.4, Cult of the Lamb 6.3 |
| 5 | Mais inimigos (explosivo, escudeiro, invocador) e um chefe | — |
| 6 | Progressão entre runs | Hades 7.5 |
| 7 | Direção de arte, animações, som e menus | — |

## Histórico de versões

| Etiqueta | O que entrou |
| --- | --- |
| `v1-prototipo-combate` | Sala, jogador (andar, mirar, golpe, dash), inimigo corpo a corpo, ondas, testes |
| `v2-inimigos-mago` | Base de inimigos, Guerreiro e Mago, projétil, boneco do projeto, câmera afastada |
