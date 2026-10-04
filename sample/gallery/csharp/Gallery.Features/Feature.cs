using System;

namespace VulkanCAD.Gallery
{
    // 기능 샘플 하나. Features/ 폴더의 파일 하나가 이 클래스 하나다.
    //
    // 기능은 화면(WinForms/WPF)을 모른다 — 버튼·슬라이더가 필요하면 IFeatureUi 에 "말만" 하고
    // 그리는 건 호스트가 한다. 그래서 같은 기능 파일이 WinForms·WPF 갤러리에서 그대로 돈다.
    //
    // 흐름: 호스트가 장면을 비우고(Scene.Reset) → Enter → (매 프레임 Update) → Leave
    public abstract class Feature
    {
        public abstract string Category { get; }   // 리본 탭
        public virtual string Group => Category;   // 탭 안의 그룹(이름 띠)
        public abstract string Icon { get; }       // 리본 큰 버튼 글리프(유니코드 한 글자)
        public abstract string Title { get; }
        public abstract string Summary { get; }    // 무엇을 보여 주는지 2~3줄
        public abstract string[] Apis { get; }     // 이 샘플에서 핵심인 C API 이름

        // 소스를 찾아가기 쉽게 — 클래스 이름 = 파일 이름 규칙.
        public string SourceFile => $"gallery/csharp/Gallery.Features/Features/{GetType().Name}.cs";

        protected IFeatureUi Ui { get; private set; }

        public void Enter(IFeatureUi ui)
        {
            Ui = ui;
            Setup();
        }

        // 장면을 만들고 조작 UI 를 등록한다. 장면은 이미 비어 있다.
        protected abstract void Setup();

        // 자동 시연 — 버튼으로 하는 핵심 동작을 차례로 실행해 결과를 보여 준다.
        // 호스트가 "▶ 시연" 버튼으로 노출하고, 스크린샷 모드도 이걸 부른다.
        public virtual void Demo() { }
        public bool HasDemo => GetType().GetMethod(nameof(Demo)).DeclaringType != typeof(Feature);

        // 매 프레임(CAD_Tick 직전). 애니메이션·폴링이 필요한 기능만 쓴다.
        protected virtual void Update(double dtSeconds) { }

        // 다른 기능으로 넘어갈 때. 콜백 해제·리스너 제거 등 엔진에 남긴 것을 거둔다.
        public virtual void Leave() { _later.Clear(); }

        protected void Log(string line) => Ui.Log(line);

        // ── 프레임 지연 ──
        // CAD_RequestXxx 는 큐에 들어가 **다음 틱**에 실행된다. 그 결과를 보고 다음 단계를 하려면
        // 몇 프레임 뒤로 미뤄야 한다(예: 뷰를 바꾼 뒤 그 뷰 평면에 배열). 호스트가 매 프레임 Tick 을 부른다.
        readonly System.Collections.Generic.List<(int frames, Action action)> _later =
            new System.Collections.Generic.List<(int, Action)>();

        protected void Later(int frames, Action action) => _later.Add((frames, action));

        // ── 장면 꾸미기 도우미 (예제를 짧게) ──
        // 색 입히기 — id 를 그대로 돌려줘 만들기 호출에 감쌀 수 있다: Tint(CAD_CreateBox(...), 1, 0, 0)
        protected static uint Tint(uint id, float r, float g, float b)
        {
            if (id != 0) CadApi.CAD_SetColor(id, r, g, b);
            return id;
        }

        // 상자를 바닥에 — CAD_CreateBox 의 (x,y,z) 는 **상자 중심**이다(다른 솔리드는 밑면 중심).
        // 밑면을 z 에 놓으려면 높이의 절반만큼 올려서 만든다.
        protected static uint Box(float x, float y, float sx, float sy, float sz, float z = 0) =>
            CadApi.CAD_CreateBox(x, y, z + sz / 2, sx, sy, sz);

        // 바닥(XY)에 놓는 설명 글자
        protected static uint Caption(float x, float y, string text, float height = 0.3f) =>
            CadApi.CAD_CreateText(x, y, 0, text, height);

        // 실패한 만들기를 로그로 — 0 이면 엔진이 남긴 이유를 같이 보여 준다
        protected uint Check(string what, uint id)
        {
            Log(id != 0 ? $"{what} → id {id}" : $"{what}: 실패 — {Cad.LastError()}");
            return id;
        }

        public void Tick(double dtSeconds)
        {
            for (int i = 0; i < _later.Count; i++)
            {
                var (n, a) = _later[i];
                if (n <= 0) { _later.RemoveAt(i--); a(); }
                else _later[i] = (n - 1, a);
            }
            Update(dtSeconds);
        }
    }

    // 기능이 호스트에게 요청하는 UI. 호스트(WinForms/WPF)가 자기 컨트롤로 구현한다.
    public interface IFeatureUi
    {
        void Section(string title);
        void Note(string text);
        void Button(string label, Action onClick);
        IValue<float> Slider(string label, float min, float max, float value, Action<float> onChange, int decimals = 1);
        IValue<bool> Toggle(string label, bool value, Action<bool> onChange);
        IValue<int> Choice(string label, string[] items, int index, Action<int> onChange);
        // 한 줄 입력 + 실행 버튼. 명령·AI 프롬프트·문자 편집처럼 글을 받는 기능용.
        IValue<string> Input(string label, string value, string buttonText, Action<string> onSubmit);

        void Log(string line);
        void ClearLog();

        // 파일 대화상자 — filter 는 "설명|*.a;*.b" 꼴. 취소하면 null.
        string PickOpenFile(string filter);
        string PickSaveFile(string filter, string defaultFileName);

        // 실행 파일 폴더(= 런타임 에셋 경로). models/ textures/ effects/ 가 여기 있다.
        string AssetDir { get; }
    }

    // 코드에서 값을 바꿔 컨트롤에 보여 줄 때 쓴다(예: 자동 재생 중인 슬라이더).
    // Value 를 대입해도 onChange 는 불리지 않는다 — 되먹임 고리를 막기 위해.
    public interface IValue<T>
    {
        T Value { get; set; }
    }
}
