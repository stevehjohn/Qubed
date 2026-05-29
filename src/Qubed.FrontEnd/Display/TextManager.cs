using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Qubed.FrontEnd.Display;

public class TextManager
{
    private SpriteFont _font;

    public void LoadContent(ContentManager contentManager)
    {
        _font = contentManager.Load<SpriteFont>("font");
    }

    private void DrawMessage(SpriteBatch spriteBatch, string message, int left, int top)
    {
        if (message == null)
        {
            return;
        }

        for (var y = -2; y < 3; y++)
        {
            for (var x = -2; x < 3; x++)
            {
                spriteBatch.DrawString(_font, message, new Vector2(left + x, top + y), Color.Black, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, .6f);
            }
        }

        spriteBatch.DrawString(_font, message, new Vector2(left, top), Color.FromNonPremultiplied(255, 255, 255, 255), 0, Vector2.Zero, Vector2.One, SpriteEffects.None, .7f);    
    }
}