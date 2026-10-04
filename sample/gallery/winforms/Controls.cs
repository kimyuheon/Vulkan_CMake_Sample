using System;
using System.Drawing;
using System.Windows.Forms;

namespace VulkanCAD.Gallery.WinForms
{
    // IValue<T> 를 WinForms 컨트롤 하나에 잇는 얇은 껍데기.
    // 코드에서 Value 를 넣을 땐 Setting 이 켜져 있어 컨트롤 이벤트가 기능으로 되돌아가지 않는다.
    sealed class ValueBox<T> : IValue<T>
    {
        readonly Func<T> _get;
        readonly Action<T> _set;
        public bool Setting { get; private set; }

        public ValueBox(Func<T> get, Action<T> set) { _get = get; _set = set; }

        public T Value
        {
            get => _get();
            set { Setting = true; try { _set(value); } finally { Setting = false; } }
        }
    }

    // 라벨 + 값 + 트랙바 한 줄. TrackBar 는 정수뿐이라 0..Steps 로 나눠 실수에 잇는다.
    sealed class SliderRow : Panel, IValue<float>
    {
        const int Steps = 1000;
        readonly float _min, _max;
        readonly string _fmt;
        readonly Label _name = new Label { AutoSize = true, ForeColor = Theme.TextDim, Location = new Point(0, 2) };
        readonly Label _value = new Label { AutoSize = true, ForeColor = Theme.Text, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        readonly TrackBar _bar = new TrackBar { Minimum = 0, Maximum = Steps, TickStyle = TickStyle.None, TabStop = false };
        bool _setting;

        public event Action<float> Changed;

        public SliderRow(string label, float min, float max, float value, int decimals, int width)
        {
            _min = min; _max = max;
            _fmt = "0." + new string('#', Math.Max(1, decimals));
            Width = width; Height = 50; Margin = new Padding(0, 2, 0, 2);
            _name.Text = label;
            _bar.SetBounds(-6, 18, width + 12, 30);
            Controls.AddRange(new Control[] { _name, _value, _bar });
            Value = value;
            _bar.ValueChanged += (s, e) =>
            {
                ShowValue();
                if (!_setting) Changed?.Invoke(Value);
            };
        }

        public float Value
        {
            get => _min + (_max - _min) * _bar.Value / Steps;
            set
            {
                _setting = true;
                float t = _max > _min ? (value - _min) / (_max - _min) : 0;
                _bar.Value = (int)Math.Round(Math.Clamp(t, 0, 1) * Steps);
                _setting = false;
                ShowValue();
            }
        }

        void ShowValue()
        {
            _value.Text = Value.ToString(_fmt);
            _value.Location = new Point(Width - _value.Width, 2);
        }
    }
}
