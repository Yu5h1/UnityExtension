using UnityEngine;

namespace Yu5h1Lib
{
    public class MaterialTextureSheetController : ComponentController<Renderer>
    {
        [SerializeField] private string _textureProperty = "_BaseMap";
        [SerializeField] private int _columns = 4;
        [SerializeField] private int _rows = 4;
        [SerializeField] private float _fps = 12f;

        private float _timer;
        private int _frame;
        private int FrameCount => _columns * _rows;

        private void Update()
        {
            _timer += Time.deltaTime * _fps;
            int frame = (int)_timer % FrameCount;
            if (frame == _frame)
                return;
            _frame = frame;
            ApplyFrame(_frame);
        }

        private void ApplyFrame(int frame)
        {
            float scaleX = 1f / _columns;
            float scaleY = 1f / _rows;
            int col = frame % _columns;
            int row = frame / _columns;
            component.material.SetTextureScale(_textureProperty,  new Vector2(scaleX, scaleY));
            component.material.SetTextureOffset(_textureProperty, new Vector2(col * scaleX, 1f - (row + 1) * scaleY));
        }
    }
}
