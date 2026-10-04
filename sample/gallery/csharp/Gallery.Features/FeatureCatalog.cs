using System.Collections.Generic;

namespace VulkanCAD.Gallery
{
    // 갤러리에 나오는 순서 = 리본 탭·그룹 순서. 새 기능을 만들면 여기에 한 줄 추가한다.
    public static class FeatureCatalog
    {
        public static IReadOnlyList<Feature> Create() => new Feature[]
        {
            // 만들기
            new Shapes2D(),
            new TextAndAnnotation(),
            new SolidPrimitives(),
            new SketchToSolid(),
            new CustomMesh(),
            new InteractiveTools(),

            // 편집
            new Transform(),
            new Arrays(),
            new Jig(),
            new Edit2D(),
            new BooleanOps(),
            new SolidEditing(),
            new UndoTransaction(),

            // 파라메트릭
            new BRepFeatures(),
            new SketchConstraints(),

            // 객체·속성
            new ObjectQuery(),
            new LayersLinetypes(),
            new Materials(),
            new EventsAndPicking(),

            // 보기
            new ViewCamera(),
            new Section(),
            new DrawingViews(),

            // 파일
            new FilesDocuments(),
            new ImagesOcr(),

            // 로봇·시뮬
            new RobotArm(),
            new Rigging(),
            new PointCloud(),
            new ExternalPose(),
            new Particles(),
            new Navigation(),

            // 건축
            new Floorplan(),

            // 분석
            new MeshTools(),
            new ExplodeView(),

            // 확장
            new CustomCommands(),
            new PluginEntity(),
            new AiMcp(),
        };
    }
}
