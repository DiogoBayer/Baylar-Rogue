using Godot;

namespace MeuRoguelike;

// Formas e materiais provisórios. Quando houver arte de verdade, é aqui que ela entra.
public static class Visual
{
    public static StandardMaterial3D Material(Color cor, bool semLuz = false)
    {
        var m = new StandardMaterial3D { AlbedoColor = cor, Roughness = 0.8f };
        if (semLuz) m.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        if (cor.A < 1f) m.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        return m;
    }

    public static MeshInstance3D Capsula(float raio, float altura, StandardMaterial3D material)
    {
        return new MeshInstance3D
        {
            Mesh = new CapsuleMesh { Radius = raio, Height = altura },
            MaterialOverride = material,
            Position = new Vector3(0, altura / 2f, 0),
        };
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
        var material = Material(cor, semLuz: true);
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        return new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = material, Position = new Vector3(0, 0.05f, 0), Visible = false };
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
        dono.CreateTween().TweenProperty(material, "albedo_color", corNormal, 0.12f);
    }
}
