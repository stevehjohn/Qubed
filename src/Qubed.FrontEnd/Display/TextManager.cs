using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Qubed.FrontEnd.Display;

public class TextManager
{
    private readonly SpriteBatch _spriteBatch;

    private readonly SpriteFont _font;

    public TextManager(SpriteBatch spriteBatch, SpriteFont font)
    {
        _spriteBatch = spriteBatch;

        _font = font;
    }

    public void DrawMessage(string message, int left, int top, Color? color = null, bool center = false)
    {
        if (message == null)
        {
            return;
        }

        color ??= Color.White;

        if (center)
        {
            left -= (int) _font.MeasureString(message).X / 2;
        }

        for (var y = -2; y < 3; y++)
        {
            for (var x = -2; x < 3; x++)
            {
                _spriteBatch.DrawString(_font, message, new Vector2(left + x, top + y), Color.Black, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, .6f);
            }
        }

        _spriteBatch.DrawString(_font, message, new Vector2(left, top), color.Value, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, .7f);
    }
}