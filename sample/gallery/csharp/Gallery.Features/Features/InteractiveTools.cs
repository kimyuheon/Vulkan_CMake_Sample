using System;
using static VulkanCAD.CadApi;

namespace VulkanCAD.Gallery
{
    public sealed class InteractiveTools : Feature
    {
        public override string Category => "만들기";
        public override string Group => "도구";
        public override string Icon => "⌨";
        public override string Title => "대화형 도구·명령행";
        public override string Summary =>
            "화면 클릭으로 그리는 도구를 시작하고, 명령 이름으로 엔진 명령 200여 개를 실행한다. " +
            "도구가 진행 중일 때 CAD_ExecuteCommand(\"2,3\") 처럼 값을 넣으면 클릭 대신 좌표가 들어간다 — 명령행과 같은 입구.";
        public override string[] Apis => new[]
        {
            "CAD_RequestStartLineSketch", "CAD_RequestStartCircleSketch", "CAD_RequestStartPolylineSketch",
            "CAD_ExecuteCommand", "CAD_GetPrompt", "CAD_SetOnPrompt",
        };

        // 콜백 델리게이트는 필드로 붙들어 둔다 — 지역 변수면 GC 가 걷어가 엔진이 죽은 함수를 부른다.
        CAD_SetOnPrompt_cb _onPrompt;

        protected override void Setup()
        {
            _onPrompt = text => Log("안내: " + Cad.FromUtf8(text));
            CAD_SetOnPrompt(_onPrompt);
            Scene.Frame(1);

            Ui.Section("도구 시작 (이후 화면 클릭)");
            Ui.Button("선", CAD_RequestStartLineSketch);
            Ui.Button("원", CAD_RequestStartCircleSketch);
            Ui.Button("사각형", CAD_RequestStartRectangleSketch);
            Ui.Button("호 (Tab = 방식 전환)", CAD_RequestStartArcSketch);
            Ui.Button("정다각형 (Tab = 변 수)", CAD_RequestStartPolygonSketch);
            Ui.Button("폴리선", CAD_RequestStartPolylineSketch);
            Ui.Note("ESC = 취소. 그리는 중에도 아래 입력란에 좌표(예: 3,2)나 길이를 넣을 수 있다.");

            Ui.Section("명령행");
            Ui.Input("명령 또는 값", "circle", "실행", cmd =>
            {
                bool ok = CAD_ExecuteCommand(cmd);
                Log($"> {cmd}  {(ok ? "" : "(알 수 없는 명령)")}");
            });
            Ui.Note("예: line · circle · rect · offset · trim · fillet · 2,3 · @1,0 — 하단 명령행에 치는 것과 같다.");
        }

        public override void Leave()
        {
            CAD_SetOnPrompt(null);
            base.Leave();
        }

        // 선 도구를 시작하고 좌표를 값으로 넣어 삼각형을 그린다. 요청은 다음 틱에 돌므로 한 프레임씩 띄운다.
        public override void Demo()
        {
            CAD_ExecuteCommand("line");
            string[] pts = { "0,0", "4,0", "2,3", "0,0" };
            for (int i = 0; i < pts.Length; i++)
            {
                string p = pts[i];
                Later(2 + i * 2, () => { CAD_ExecuteCommand(p); Log("> " + p); });
            }
            Later(2 + pts.Length * 2, () => CAD_ExecuteCommand(""));   // 빈 입력 = Enter(끝내기)
        }
    }
}
