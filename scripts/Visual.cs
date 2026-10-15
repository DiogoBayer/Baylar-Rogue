using System.Collections.Generic;
using Godot;

namespace MeuRoguelike;

// Formas, materiais e o boneco dos personagens. Quando houver arte de verdade, é aqui que ela entra.
public static class Visual
{
    static PackedScene cenaBoneco;

    public static StandardMaterial3D Material(Color cor, bool semLuz = false)
    {
        var m = new StandardMaterial3D { AlbedoColor = cor, Roughness = 0.8f };
        if (semLuz) m.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        if (cor.A < 1f) m.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        return m;
    }

    // Material dos personagens: emite um pouco da própria cor, para não sumirem na rua escura.
    public static StandardMaterial3D MaterialPersonagem(Color cor)
    {
        return new StandardMaterial3D { AlbedoColor = cor, Roughness = 0.45f, EmissionEnabled = true, Emission = cor, EmissionEnergyMultiplier = 0.55f, RimEnabled = true, Rim = 0.6f };
    }

    public static StandardMaterial3D MaterialBrilhante(Color cor)
    {
        return new StandardMaterial3D { AlbedoColor = cor, EmissionEnabled = true, Emission = cor, EmissionEnergyMultiplier = 2.5f };
    }

    // O boneco do modelos/boneco.glb, pintado com o material e ajustado à altura,
    // com os pés em y = 0 e centralizado.
    public static Node3D Boneco(float altura, StandardMaterial3D material)
    {
        cenaBoneco ??= GD.Load<PackedScene>("res://modelos/boneco.glb");
        var boneco = cenaBoneco.Instantiate<Node3D>();

        var malhas = new List<MeshInstance3D>();
        ColetarMalhas(boneco, malhas);
        Aabb? caixa = null;
        foreach (var malha in malhas)
        {
            malha.MaterialOverride = material;
            var aabb = TransformacaoRelativa(boneco, malha) * malha.GetAabb();
            caixa = caixa is Aabb c ? c.Merge(aabb) : aabb;
        }
        if (caixa is not Aabb total) return boneco;

        float escala = altura / total.Size.Y;
        var centro = total.GetCenter();
        boneco.Scale = Vector3.One * escala;
        boneco.Position = new Vector3(-centro.X, -total.Position.Y, -centro.Z) * escala;
        return boneco;
    }

    static void ColetarMalhas(Node no, List<MeshInstance3D> malhas)
    {
        if (no is MeshInstance3D malha) malhas.Add(malha);
        foreach (var filho in no.GetChildren()) ColetarMalhas(filho, malhas);
    }

    static Transform3D TransformacaoRelativa(Node3D raiz, Node3D no)
    {
        var t = Transform3D.Identity;
        for (Node n = no; n != raiz && n is Node3D n3; n = n.GetParent())
            t = n3.Transform * t;
        return t;
    }

    // Enquanto o boneco não tem animações, ele balança e inclina ao andar.
    public static void AnimarAndar(Node3D modelo, Vector3 velocidade, Vector3 olhar, ref float fase, float delta)
    {
        velocidade.Y = 0;
        float rapidez = velocidade.Length();
        float intensidade = Mathf.Min(1f, rapidez / 3f);
        fase += delta * (4f + rapidez * 1.6f);
        float pulo = Mathf.Abs(Mathf.Sin(fase)) * 0.09f * intensidade;
        float gingado = Mathf.Sin(fase) * 0.07f * intensidade;
        modelo.Position = new Vector3(0, pulo, 0);
        modelo.Rotation = new Vector3(0.12f * intensidade, Mathf.Atan2(olhar.X, olhar.Z), gingado);
    }

    public static MeshInstance3D Caixa(Vector3 tamanho, StandardMaterial3D material)
    {
        return new MeshInstance3D { Mesh = new BoxMesh { Size = tamanho }, MaterialOverride = material };
    }

    // Leque plano no chão, apontando para +Z: mostra a área de um golpe.
    public static MeshInstance3D Leque(float raio, float anguloGraus, Color cor)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        const int partes = 14;
        float a0 = Mathf.DegToRad(-anguloGraus), a1 = Mathf.DegToRad(anguloGraus);
        for (int i = 0; i < partes; i++)
        {
            float t0 = Mathf.Lerp(a0, a1, (float)i / partes);
            float t1 = Mathf.Lerp(a0, a1, (float)(i + 1) / partes);
            st.AddVertex(Vector3.Zero);
            st.AddVertex(new Vector3(Mathf.Sin(t1), 0, Mathf.Cos(t1)) * raio);
            st.AddVertex(new Vector3(Mathf.Sin(t0), 0, Mathf.Cos(t0)) * raio);
        }
        return Plano(st, cor, visivel: false);
    }

    // Setinha no chão, à frente do personagem, apontando para +Z: mostra para onde ele mira.
    public static MeshInstance3D Seta(Color cor)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.AddVertex(new Vector3(0, 0, 1.25f));
        st.AddVertex(new Vector3(0.22f, 0, 0.85f));
        st.AddVertex(new Vector3(-0.22f, 0, 0.85f));
        return Plano(st, cor, visivel: true);
    }

    static MeshInstance3D Plano(SurfaceTool st, Color cor, bool visivel)
    {
        var material = Material(cor, semLuz: true);
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        return new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = material, Position = new Vector3(0, 0.05f, 0), Visible = visivel, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
    }

    // Mostra o leque por um instante e o apaga aos poucos.
    public static void Piscar(Node dono, MeshInstance3D leque, Color cor, float duracao = 0.15f)
    {
        var material = (StandardMaterial3D)leque.MaterialOverride;
        material.AlbedoColor = cor;
        leque.Visible = true;
        var tween = dono.CreateTween();
        tween.TweenProperty(material, "albedo_color", new Color(cor, 0f), duracao);
        tween.TweenCallback(Callable.From(() => leque.Visible = false));
    }

    // Deixa o corpo branco por um instante quando leva dano.
    public static void Flash(Node dono, StandardMaterial3D material, Color corNormal)
    {
        material.AlbedoColor = Colors.White;
        material.Emission = Colors.White;
        var tween = dono.CreateTween().SetParallel();
        tween.TweenProperty(material, "albedo_color", corNormal, 0.12f);
        tween.TweenProperty(material, "emission", corNormal, 0.12f);
    }
}
