using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Qubed.Core;
using Qubed.Core.Extensions;
using Qubed.Core.Infrastructure;
using Qubed.Core.Logic;
using Qubed.Core.Models;
using Cube = Qubed.Core.Models.Cube;
using Move = Qubed.Core.Models.Move;

namespace Qubed.FrontEnd.Display;

public sealed class Qubed : Game
{
    private const int WindowWidth = 800;

    private const int WindowHeight = 520;

    private const int ViewportWidth = 528;

    private const int ViewportHeight = 480;

    private const int NetTileSize = 20;

    private const int NetSpacing = 6;

    private const int PanelWidth = NetTileSize * 12 + NetSpacing * 13;

    private const int PanelHeight = NetTileSize * 9 + NetSpacing * 10;

    private const float CameraDistance = 9.95f;

    private const int NetLeft = 455;

    private const int NetTop = 120;

    private const float CubieSize = 1f;

    private const float CubeSpacingFinal = 1.25f;

    private const float QuarterTurn = MathHelper.PiOver2;

    private const float MouseRotationScale = 0.01f;

    private const float CubePickHalfExtent = CubeSpacingFinal + CubieSize / 2f + 0.05f;

    private const float StickerInset = 0.08f;

    private const float StickerOffset = 0.015f;

    private const float StickerThickness = 0.07f;

    private const float RotationDuration = 0.25f;

    private const float ScrambleRotationDuration = 0.1f;

    private const float SolveAnimationSeconds = 10f;

    private const int ProgressGraceMoves = 20;

    private const int ProgressBarWidth = 100;

    private const int ProgressBarHeight = 20;

    private const int ProgressBarLeft = 225 - ProgressBarWidth / 2;

    private const int ProgressBarTop = 430;

    private const int ProgressBarBorderWidth = 4;

    // ReSharper disable once NotAccessedField.Local
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly GraphicsDeviceManager _graphics;

    private readonly List<Cubie> _cubies = [];

    private readonly Queue<QueueMove> _solveQueue = [];

    private readonly Random _random = new();

    private readonly Cube _cube = new();

    private readonly Color[] _netData = new Color[PanelWidth * PanelHeight];

    private readonly Color[] _progressData = new Color[ProgressBarWidth * ProgressBarHeight];

    private readonly Lock _solveLock = new();

    private readonly ILogger _logger;

    private readonly Color[] _faceColors =
    [
        Color.White,
        Color.Yellow,
        Color.Red,
        Color.Orange,
        Color.Green,
        Color.Blue
    ];

    private readonly Stopwatch _stopwatch = new();

    private TextManager _textManager;

    private BasicEffect _effect;

    private Matrix _view;

    private Viewport _cubeViewport;

    private Matrix _primitiveTransform = Matrix.Identity;

    private KeyboardState _previousKeyboard;

    private MouseState _previousMouse;

    private MouseDragMode _mouseDragMode;

    private FaceRotation _activeRotation;

    private bool _isSolving;

    private bool _isScrambling;

    private float _yaw = -0.789994895f;

    private float _pitch = 0.490001917f;

    private int _scrambleTurns;

    private float _rotationDuration = RotationDuration;

    private bool _solverFinished;

    private float _cubeSpacing = 9f;

    private float _cubeSpacingSpeed = 0.1f;

    private Face? _previousFace1;

    private Face? _previousFace2;

    private SpriteBatch _spriteBatch;

    private Texture2D _netTexture;

    private Texture2D _progressTexture;

    private bool _isUndo;

    private bool _isUndoRedo;

    private char? _consoleKey;

    private bool _victoryActive;

    private float _victoryTime;

    private float _victoryYaw;

    private float _victoryPitch;

    private float _victorySpacingOffset;

    private SoundEffect _clickSound;

    private SoundEffect _solvedSound;

    private Dictionary<Face, Face> _faceMappings;

    private bool _resetOnNextUserMove;

    private string _solverStage;

    private float _thinkingPause;

    private int _progress;

    private int _progressGraceMoves;

    public Qubed(ILogger logger = null)
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = WindowWidth,
            PreferredBackBufferHeight = WindowHeight
        };

        IsMouseVisible = true;

        _graphics.PreferMultiSampling = true;

        _logger = logger;
    }

    protected override void Initialize()
    {
        Window.Title = "Qubed";

        CreateSolvedCube();

        _previousKeyboard = Keyboard.GetState();

        _previousMouse = Mouse.GetState();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        Content.RootDirectory = "Content";

        _effect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };

        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _textManager = new TextManager(_spriteBatch, Content.Load<SpriteFont>("font"));

        _netTexture = new Texture2D(GraphicsDevice, PanelWidth, PanelHeight);

        _progressTexture = new Texture2D(GraphicsDevice, ProgressBarWidth, ProgressBarHeight);

        _clickSound = Content.Load<SoundEffect>("click");

        _solvedSound = Content.Load<SoundEffect>("solved");

        UpdateView();

        _cubeViewport = GetCubeViewport();
    }

    protected override void Update(GameTime gameTime)
    {
        if (_thinkingPause > 0)
        {
            _thinkingPause -= (float) gameTime.ElapsedGameTime.TotalSeconds;
        }

        if (_cubeSpacing > CubeSpacingFinal)
        {
            _cubeSpacing -= _cubeSpacingSpeed;

            _cubeSpacingSpeed += 0.01f;
        }
        else
        {
            _cubeSpacing = CubeSpacingFinal;
        }

        if (Console.KeyAvailable)
        {
            _consoleKey = Console.ReadKey(true).KeyChar;
        }
        else
        {
            _consoleKey = null;
        }

        var keyboard = Keyboard.GetState();

        var mouse = Mouse.GetState();

        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (keyboard.IsKeyDown(Keys.Left))
        {
            _yaw -= 0.02f;

            UpdateView();
        }

        if (keyboard.IsKeyDown(Keys.Right))
        {
            _yaw += 0.02f;

            UpdateView();
        }

        if (keyboard.IsKeyDown(Keys.Up))
        {
            _pitch -= 0.02f;

            UpdateView();
        }

        if (keyboard.IsKeyDown(Keys.Down))
        {
            _pitch += 0.02f;

            UpdateView();
        }

        UpdateMouseControls(mouse);

        UpdateActiveRotation(gameTime);

        if (_isSolving)
        {
            if (_activeRotation == null)
            {
                StartNextSolveRotation();
            }
        }

        TryStartSolveAnimation(keyboard);

        TryStartFaceRotation(keyboard);

        TryScramble(keyboard);

        _previousKeyboard = keyboard;

        _previousMouse = mouse;

        if (_victoryActive)
        {
            _victoryTime += (float) gameTime.ElapsedGameTime.TotalSeconds;

            _yaw = _victoryYaw + MathHelper.TwoPi * _victoryTime;

            _pitch = _victoryPitch + MathF.Sin(_victoryTime * 8f) * 0.15f;

            var progress = MathHelper.Clamp(_victoryTime / 1.25f, 0f, 1f);

            var fade = 1f - progress;

            _victorySpacingOffset =
                MathF.Sin(progress * MathHelper.TwoPi * 2f) *
                fade *
                0.35f;

            UpdateView();

            if (_victoryTime >= 1.25f)
            {
                _victoryActive = false;
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.FromNonPremultiplied(70, 70, 70, 255));

        GraphicsDevice.RasterizerState = RasterizerState.CullNone;

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        var fullViewport = GraphicsDevice.Viewport;

        GraphicsDevice.Viewport = _cubeViewport;

        _effect.World = Matrix.Identity;

        _effect.View = _view;

        _effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), _cubeViewport.AspectRatio, 0.1f, 100f);

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();

            DrawQube();
        }

        GraphicsDevice.Viewport = fullViewport;

        _effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), fullViewport.AspectRatio, 0.1f, 100f);

        _spriteBatch.Begin(SpriteSortMode.FrontToBack);

        DrawNet();

        UpdateText(gameTime);

        DrawProgress();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdateText(GameTime gameTime)
    {
        if (_stopwatch.Elapsed == TimeSpan.Zero)
        {
            _textManager.DrawMessage("Moves: -", NetLeft + PanelWidth / 4, 20);

            _textManager.DrawMessage("Time: -", NetLeft + PanelWidth / 4, 60);
        }
        else
        {
            _textManager.DrawMessage($"Moves: {_cube.MoveCount}", NetLeft + PanelWidth / 4, 20);

            _textManager.DrawMessage($@"Time: {_stopwatch.Elapsed:mm\:ss\.ff}", NetLeft + PanelWidth / 4, 60);
        }

        var time = (float) gameTime.TotalGameTime.TotalSeconds;

        var textColour = (byte) (127 + MathF.Sin(time * MathHelper.TwoPi) * 100);

        if (_isScrambling)
        {
            _textManager.DrawMessage("Scrambling!", 220, 20, Color.FromNonPremultiplied(0xFF, textColour, textColour, 0xFF), true);
        }

        if (_isSolving)
        {
            if (_activeRotation == null)
            {
                _textManager.DrawMessage("Thinking...", 220, 20, Color.FromNonPremultiplied(0xFF, textColour, 0xFF, 0xFF), true);
            }
            else
            {
                _textManager.DrawMessage("Solving!", 220, 20, Color.FromNonPremultiplied(0xFF, 0xFF, textColour, 0xFF), true);
            }
        }

        if (_cube.IsSolved() && _stopwatch.Elapsed > TimeSpan.Zero && ! _stopwatch.IsRunning)
        {
            _textManager.DrawMessage("Solved!", 220, 20, Color.FromNonPremultiplied(textColour, 0xFF, textColour, 0xFF), true);
        }

        if (! string.IsNullOrWhiteSpace(_solverStage))
        {
            _textManager.DrawMessage(_solverStage, Window.ClientBounds.Width / 2, 470, Color.FromNonPremultiplied(textColour, 0xFF, textColour, 0xFF), true);
        }
    }

    private void DrawProgress()
    {
        var progress = GetProgressWithGrace();

        Array.Fill(_progressData, Color.Transparent);

        for (var y = 0; y < ProgressBarBorderWidth; y++)
        {
            for (var x = 0; x < ProgressBarWidth; x++)
            {
                _progressData[y * ProgressBarWidth + x] = Color.Black;

                _progressData[(ProgressBarHeight - 1 - y) * ProgressBarWidth + x] = Color.Black;
            }
        }

        for (var x = 0; x < ProgressBarBorderWidth; x++)
        {
            for (var y = 0; y < ProgressBarHeight; y++)
            {
                _progressData[y * ProgressBarWidth + x] = Color.Black;

                _progressData[y * ProgressBarWidth + (ProgressBarWidth - 1 - x)] = Color.Black;
            }
        }

        const int innerRight = ProgressBarWidth - ProgressBarBorderWidth;
        
        const int innerBottom = ProgressBarHeight - ProgressBarBorderWidth;

        const int innerWidth = innerRight - ProgressBarBorderWidth;

        var barLength = innerWidth * progress / 8;

        for (var y = ProgressBarBorderWidth; y < innerBottom; y++)
        {
            for (var x = ProgressBarBorderWidth; x < ProgressBarBorderWidth + barLength; x++)
            {
                _progressData[y * ProgressBarWidth + x] = Color.FromNonPremultiplied(0x00, 0xD0, 0x00, 0xFF);
            }
        }

        _progressTexture.SetData(_progressData);

        _spriteBatch.Draw(_progressTexture, new Vector2(ProgressBarLeft, ProgressBarTop), new Rectangle(0, 0, ProgressBarWidth, ProgressBarHeight), Color.White);
    }

    private int GetProgressWithGrace()
    {
        if (_isScrambling)
        {
            return _progress;
        }

        var progress = GetProgress();

        if (progress > _progress || _progressGraceMoves == 0)
        {
            _progress = progress;

            _progressGraceMoves = ProgressGraceMoves;
        }

        return _progress;
    }

    private int GetProgress()
    {
        var progress = 0;

        for (var i = 0; i < AlgorithmLibrary.Algorithms.Count - 1; i++)
        {
            if (! AlgorithmLibrary.Algorithms[i].IsCompleteChecks(_cube))
            {
                return progress;
            }

            progress++;
        }

        if (! (_cube[Face.Down, 1, 0] == Colour.Yellow
               && _cube[Face.Down, 2, 1] == Colour.Yellow
               && _cube[Face.Down, 1, 2] == Colour.Yellow
               && _cube[Face.Down, 0, 1] == Colour.Yellow))
        {
            return progress;
        }

        progress++;

        if (! (_cube[Face.Down, 0, 0] == Colour.Yellow
               && _cube[Face.Down, 2, 0] == Colour.Yellow
               && _cube[Face.Down, 0, 2] == Colour.Yellow
               && _cube[Face.Down, 2, 2] == Colour.Yellow))
        {
            return progress;
        }

        progress++;

        if (! _cube.IsSolved())
        {
            return progress;
        }

        return progress + 1;
    }

    private void DrawNet()
    {
        const int unit = NetTileSize + NetSpacing;

        DrawFace(Face.Up, NetSpacing + unit * 3, NetSpacing);

        DrawFace(Face.Left, NetSpacing, NetSpacing + unit * 3);

        DrawFace(Face.Front, NetSpacing + unit * 3, NetSpacing + unit * 3);

        DrawFace(Face.Right, NetSpacing + unit * 6, NetSpacing + unit * 3);

        DrawFace(Face.Back, NetSpacing + unit * 9, NetSpacing + unit * 3);

        DrawFace(Face.Down, NetSpacing + unit * 3, NetSpacing + unit * 6);

        _netTexture.SetData(_netData);

        _spriteBatch.Draw(_netTexture, new Vector2(NetLeft, NetTop), new Rectangle(0, 0, PanelWidth, PanelHeight), Color.White);
    }

    private void DrawFace(Face face, int left, int top)
    {
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                DrawBorder(left + x * (NetTileSize + NetSpacing), top + y * (NetTileSize + NetSpacing));

                DrawTile(ToColor(_cube[face, x, y]), left + x * (NetTileSize + NetSpacing), top + y * (NetTileSize + NetSpacing));
            }
        }
    }

    private void DrawBorder(int left, int top)
    {
        var right = left + NetTileSize;

        var bottom = top + NetTileSize;

        for (var y = top - NetSpacing; y < bottom + NetSpacing; y++)
        {
            for (var x = left - NetSpacing; x < right + NetSpacing; x++)
            {
                if (x < 0 || y < 0 || x >= PanelWidth || y >= PanelHeight)
                {
                    continue;
                }

                if (x >= left && x < right && y >= top && y < bottom)
                {
                    continue;
                }

                _netData[x + y * PanelWidth] = Color.Black;
            }
        }
    }

    private void DrawTile(Color color, int left, int top)
    {
        for (var y = 0; y < NetTileSize; y++)
        {
            for (var x = 0; x < NetTileSize; x++)
            {
                _netData[left + x + (top + y) * PanelWidth] = color;
            }
        }
    }

    private void UpdateMouseControls(MouseState mouse)
    {
        if (! IsActive)
        {
            return;
        }

        var leftWasReleased = _previousMouse.LeftButton == ButtonState.Released;

        var leftIsPressed = mouse.LeftButton == ButtonState.Pressed;

        var leftIsReleased = mouse.LeftButton == ButtonState.Released;

        if (leftIsReleased)
        {
            _mouseDragMode = MouseDragMode.None;

            return;
        }

        if (leftIsPressed && leftWasReleased)
        {
            _mouseDragMode = IsMouseInsideClientArea(mouse) && TryStartMouseFaceRotation(mouse)
                ? MouseDragMode.FaceTurn
                : IsMouseInsideClientArea(mouse)
                    ? MouseDragMode.Orbit
                    : MouseDragMode.None;
        }

        if (leftIsPressed && _previousMouse.LeftButton == ButtonState.Pressed && _mouseDragMode == MouseDragMode.Orbit)
        {
            var viewSign = ViewSign();

            _yaw -= (mouse.X - _previousMouse.X) * MouseRotationScale * viewSign;

            _pitch += (mouse.Y - _previousMouse.Y) * MouseRotationScale;

            UpdateView();
        }
    }

    private bool IsMouseInsideClientArea(MouseState mouse)
    {
        var viewport = GraphicsDevice.Viewport;

        return mouse.X >= 0
               && mouse.Y >= 0
               && mouse.X < viewport.Width
               && mouse.Y < viewport.Height;
    }

    private bool TryStartMouseFaceRotation(MouseState mouse)
    {
        if (_activeRotation is not null || _isSolving || ! TryPickCubeFace(mouse, out var face))
        {
            return false;
        }

        var keyboard = Keyboard.GetState();

        var direction = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift)
            ? Direction.AntiClockwise
            : keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl)
                ? Direction.HalfTurn
                : Direction.Clockwise;

        StartFaceRotation(new Move(face, direction));

        return true;
    }

    private bool TryPickCubeFace(MouseState mouse, out Face face)
    {
        var viewport = GetCubeViewport();

        if (mouse.X < viewport.X || mouse.Y < viewport.Y || mouse.X >= viewport.X + viewport.Width || mouse.Y >= viewport.Y + viewport.Height)
        {
            face = Face.Front;

            return false;
        }

        var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), viewport.AspectRatio, 0.1f, 100f);

        var nearPoint = viewport.Unproject(new Vector3(mouse.X, mouse.Y, 0f), projection, _view, Matrix.Identity);

        var farPoint = viewport.Unproject(new Vector3(mouse.X, mouse.Y, 1f), projection, _view, Matrix.Identity);

        var ray = new Ray(nearPoint, Vector3.Normalize(farPoint - nearPoint));

        var bounds = new BoundingBox(new Vector3(-CubePickHalfExtent, -CubePickHalfExtent, -CubePickHalfExtent), new Vector3(CubePickHalfExtent, CubePickHalfExtent, CubePickHalfExtent));

        var distance = ray.Intersects(bounds);

        if (! distance.HasValue)
        {
            face = Face.Front;

            return false;
        }

        var hit = ray.Position + ray.Direction * distance.Value;

        face = FaceFromHitPoint(hit);

        return true;
    }

    private void MapFacesToDirection()
    {
        var normals = new List<(Face Face, Vector3 Normal)>();

        foreach (var face in Enum.GetValues<Face>())
        {
            normals.Add((face, Vector3.TransformNormal(NormalForFace(face), _view)));
        }

        var front = normals.MaxBy(n => n.Normal.Z).Face;

        var up = normals
            .Where(n => n.Face != front &&
                        n.Face != front.Opposite())
            .MaxBy(n => n.Normal.Y).Face;

        var right = normals
            .Where(n => n.Face != front &&
                        n.Face != front.Opposite() &&
                        n.Face != up &&
                        n.Face != up.Opposite())
            .MaxBy(n => n.Normal.X).Face;

        _faceMappings = new Dictionary<Face, Face>
        {
            { Face.Up, up },
            { Face.Down, up.Opposite() },
            { Face.Front, front },
            { Face.Back, front.Opposite() },
            { Face.Left, right.Opposite() },
            { Face.Right, right }
        };
    }

    private Viewport GetCubeViewport()
    {
        return new Viewport(-40, -15, ViewportWidth, ViewportHeight);
    }

    private static Face FaceFromHitPoint(Vector3 hit)
    {
        var absX = MathF.Abs(hit.X);

        var absY = MathF.Abs(hit.Y);

        var absZ = MathF.Abs(hit.Z);

        if (absX >= absY && absX >= absZ)
        {
            return hit.X < 0 ? Face.Left : Face.Right;
        }

        if (absY >= absZ)
        {
            return hit.Y < 0 ? Face.Down : Face.Up;
        }

        return hit.Z < 0 ? Face.Back : Face.Front;
    }

    private void UpdateView()
    {
        const float epsilon = 0.0001f;

        var pitch = _pitch;

        if (MathF.Abs(MathF.Cos(pitch)) < epsilon)
        {
            pitch += MathF.Sign(MathF.Sin(pitch)) * epsilon;
        }

        var x = MathF.Cos(pitch) * MathF.Sin(_yaw);

        var y = MathF.Sin(pitch);

        var z = MathF.Cos(pitch) * MathF.Cos(_yaw);

        var cameraPosition = new Vector3(x, y, z) * CameraDistance;

        var up = MathF.Cos(pitch) >= 0f ? Vector3.Up : Vector3.Down;

        _view = Matrix.CreateLookAt(cameraPosition, Vector3.Zero, up);

        MapFacesToDirection();
    }

    private int ViewSign()
    {
        return MathF.Cos(_pitch) >= 0f ? 1 : -1;
    }

    private void CreateSolvedCube()
    {
        _cubies.Clear();

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                for (var z = -1; z <= 1; z++)
                {
                    var cubie = new Cubie(new Vector3(x, y, z));

                    switch (y)
                    {
                        case 1:
                            cubie.Stickers.Add(new Sticker(Face.Up, Vector3.Up, _faceColors[0]));
                            break;
                        case -1:
                            cubie.Stickers.Add(new Sticker(Face.Down, Vector3.Down, _faceColors[1]));
                            break;
                    }

                    switch (z)
                    {
                        case 1:
                            cubie.Stickers.Add(new Sticker(Face.Front, new Vector3(0, 0, 1), _faceColors[2]));
                            break;
                        case -1:
                            cubie.Stickers.Add(new Sticker(Face.Back, new Vector3(0, 0, -1), _faceColors[3]));
                            break;
                    }

                    switch (x)
                    {
                        case -1:
                            cubie.Stickers.Add(new Sticker(Face.Left, Vector3.Left, _faceColors[4]));
                            break;
                        case 1:
                            cubie.Stickers.Add(new Sticker(Face.Right, Vector3.Right, _faceColors[5]));
                            break;
                    }

                    _cubies.Add(cubie);
                }
            }
        }
    }

    private void UpdateActiveRotation(GameTime gameTime)
    {
        if (_activeRotation is not { } rotation)
        {
            return;
        }

        if (rotation.Direction == Direction.HalfTurn)
        {
            rotation.Elapsed += (float) (gameTime.ElapsedGameTime.TotalSeconds / 1.4);
        }
        else
        {
            rotation.Elapsed += (float) gameTime.ElapsedGameTime.TotalSeconds;
        }

        if (rotation.Direction == Direction.HalfTurn && ! rotation.MidClickPlayed && rotation.Elapsed >= _rotationDuration / 2f)
        {
            rotation.MidClickPlayed = true;

            var pitch = (float) (_random.NextDouble() * 0.3 - 0.15);

            var volume = _isScrambling || _isSolving ? 0.4f : 1f;

            _clickSound.Play(volume, pitch, 0f);
        }

        if (rotation.Elapsed < _rotationDuration)
        {
            return;
        }

        CompleteFaceRotation(rotation);

        _activeRotation = null;

        if (_isSolving)
        {
            StartNextSolveRotation();
        }
    }

    private void TryStartFaceRotation(KeyboardState keyboard)
    {
        if (_activeRotation is not null || _isSolving)
        {
            return;
        }

        var direction = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift)
            ? Direction.AntiClockwise
            : Direction.Clockwise;

        if (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl))
        {
            direction = Direction.HalfTurn;
        }

        if (WasKeyPressed(keyboard, Keys.U))
        {
            StartFaceRotation(new Move(_faceMappings[Face.Up], direction));
        }
        else if (WasKeyPressed(keyboard, Keys.D))
        {
            StartFaceRotation(new Move(_faceMappings[Face.Down], direction));
        }
        else if (WasKeyPressed(keyboard, Keys.F))
        {
            StartFaceRotation(new Move(_faceMappings[Face.Front], direction));
        }
        else if (WasKeyPressed(keyboard, Keys.B))
        {
            StartFaceRotation(new Move(_faceMappings[Face.Back], direction));
        }
        else if (WasKeyPressed(keyboard, Keys.L))
        {
            StartFaceRotation(new Move(_faceMappings[Face.Left], direction));
        }
        else if (WasKeyPressed(keyboard, Keys.R))
        {
            StartFaceRotation(new Move(_faceMappings[Face.Right], direction));
        }
        else if (WasKeyPressed(keyboard, Keys.Z) && (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl)))
        {
            if (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift))
            {
                RedoMove();
            }
            else
            {
                UndoMove();
            }
        }
    }

    private void UndoMove()
    {
        var move = _cube.UndoMove();

        if (move != null)
        {
            _isUndoRedo = true;

            _isUndo = true;

            StartFaceRotation(move.Value);
        }
    }

    private void RedoMove()
    {
        var move = _cube.RedoMove();

        if (move != null)
        {
            _isUndoRedo = true;

            StartFaceRotation(move.Value);
        }
    }

    private void TryScramble(KeyboardState keyboard)
    {
        if (_activeRotation is not null || _isSolving)
        {
            return;
        }

        if (_scrambleTurns == 0)
        {
            if (WasKeyPressed(keyboard, Keys.S, 's'))
            {
                _progress = 0;

                _progressGraceMoves = 0;

                _previousFace1 = null;

                _previousFace2 = null;

                _scrambleTurns = 20;

                _rotationDuration = ScrambleRotationDuration;

                _isScrambling = true;

                _cube.ResetMoveCount();

                _stopwatch.Reset();
            }
            else
            {
                return;
            }
        }

        Face face;

        do
        {
            face = (Face) _random.Next(6);
        } while (face == _previousFace1
                 // ReSharper disable once PossibleInvalidOperationException
                 || (face == _previousFace2 && _previousFace1.Value == _previousFace2.Value.Opposite()));

        _previousFace2 = _previousFace1;

        _previousFace1 = face;

        StartFaceRotation(new Move(face, (Direction) _random.Next(3)));
    }

    private void TryStartSolveAnimation(KeyboardState keyboard)
    {
        if (_activeRotation is not null || _isSolving || _scrambleTurns > 0 || ! WasKeyPressed(keyboard, Keys.Space, ' '))
        {
            return;
        }

        FindSolveMoves();
    }

    private void FindSolveMoves()
    {
        var cube = GetCubeFromState();

        if (cube.IsSolved())
        {
            return;
        }

        var solver = new Solver(cube, Mode.Fast, _logger);

        lock (_solveLock)
        {
            _solveQueue.Clear();
        }

        _isSolving = true;

        _solverFinished = false;

        solver.SolveAsync(SolvedCallback, StepCallback);
    }

    private Cube GetCubeFromState()
    {
        var cube = new Cube();

        foreach (var face in Enum.GetValues<Face>())
        {
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    cube[face, x, y] = GetFaceColor(face, x, y);
                }
            }
        }

        return cube;
    }

    private void SolvedCallback((bool Solved, IReadOnlyList<Move> Moves, TimeSpan Elapsed) result)
    {
        _solverFinished = true;

        _rotationDuration = _solveQueue.Count > 0
            ? SolveAnimationSeconds / _solveQueue.Count
            : RotationDuration;

        _rotationDuration = Math.Min(_rotationDuration, RotationDuration);
    }

    private void StepCallback(List<Move> moves, string stage)
    {
        lock (_solveLock)
        {
            for (var i = 0; i < moves.Count; i++)
            {
                var pause = i == moves.Count - 1 && _random.Next(4) == 0;

                _solveQueue.Enqueue(new QueueMove(moves[i], stage, pause));
            }
        }
    }

    private Colour GetFaceColor(Face face, int x, int y)
    {
        var position = face switch
        {
            Face.Up => new Vector3(x - 1, 1, y - 1),
            Face.Front => new Vector3(x - 1, 1 - y, 1),
            Face.Left => new Vector3(-1, 1 - y, x - 1),
            Face.Down => new Vector3(x - 1, -1, 1 - y),
            Face.Right => new Vector3(1, 1 - y, 1 - x),
            Face.Back => new Vector3(1 - x, 1 - y, -1),
            _ => throw new ArgumentOutOfRangeException(nameof(face))
        };

        // ReSharper disable CompareOfFloatsByEqualityOperator
        var cubie = _cubies.Single(c =>
            c.Position.X == position.X &&
            c.Position.Y == position.Y &&
            c.Position.Z == position.Z);
        // ReSharper restore CompareOfFloatsByEqualityOperator

        return ToColour(cubie.Stickers.Single(s => s.Face == face).Color);
    }

    private static Color ToColor(Colour colour)
    {
        return colour switch
        {
            Colour.White => Color.White,
            Colour.Yellow => Color.Yellow,
            Colour.Red => Color.Red,
            Colour.Orange => Color.Orange,
            Colour.Blue => Color.Blue,
            Colour.Green => Color.Green,
            _ => throw new ArgumentOutOfRangeException(nameof(colour), colour, "Unknown sticker colour.")
        };
    }

    private static Colour ToColour(Color color)
    {
        return color switch
        {
            _ when color == Color.White => Colour.White,
            _ when color == Color.Yellow => Colour.Yellow,
            _ when color == Color.Red => Colour.Red,
            _ when color == Color.Orange => Colour.Orange,
            _ when color == Color.Blue => Colour.Blue,
            _ when color == Color.Green => Colour.Green,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown sticker colour.")
        };
    }

    private void StartNextSolveRotation()
    {
        if (_thinkingPause > 0)
        {
            return;
        }

        QueueMove queueMove;

        lock (_solveLock)
        {
            if (! _solveQueue.TryDequeue(out queueMove))
            {
                return;
            }

            _solverStage = queueMove.Stage;

            if (_solveQueue.Count < 2)
            {
                _rotationDuration = RotationDuration;
            }
        }

        if (queueMove.PauseAfter)
        {
            _thinkingPause = RotationDuration + _random.Next(20) / 10f;
        }

        StartFaceRotation(queueMove.Move);
    }

    private void StartFaceRotation(Move move)
    {
        if (_cube.MoveCount == 0 && ! _isScrambling)
        {
            _stopwatch.Restart();
        }

        if (_resetOnNextUserMove && ! _isScrambling && ! _isSolving)
        {
            _cube.ResetMoveCount();

            _stopwatch.Restart();

            _resetOnNextUserMove = false;
        }

        _activeRotation = new FaceRotation(move);

        var pitch = (float) (_random.NextDouble() * 0.3 - 0.15);

        var volume = _isScrambling || _isSolving ? 0.4f : 1f;

        _clickSound.Play(volume, pitch, 0f);
    }

    private bool WasKeyPressed(KeyboardState keyboard, Keys key, char? character = null)
    {
        if (_consoleKey.HasValue && character.HasValue && char.ToLower(_consoleKey.Value) == char.ToLower(character.Value))
        {
            return true;
        }

        return keyboard.IsKeyDown(key) && ! _previousKeyboard.IsKeyDown(key);
    }

    private void CompleteFaceRotation(FaceRotation rotation)
    {
        var turn = CreateTurnMatrix(rotation.Face, rotation.Direction, QuarterTurn);

        foreach (var cubie in _cubies.Where(cubie => IsCubieOnFace(cubie, rotation.Face)))
        {
            cubie.Position = RoundToGrid(Vector3.Transform(cubie.Position, turn));

            foreach (var sticker in cubie.Stickers)
            {
                sticker.Normal = RoundToGrid(Vector3.TransformNormal(sticker.Normal, turn));

                sticker.Face = FaceFromNormal(sticker.Normal);
            }
        }

        if (! _isUndoRedo)
        {
            _cube.ApplyMove(_activeRotation.Face, _activeRotation.Direction, ! _isScrambling);
        }

        if (_cube.IsSolved())
        {
            _stopwatch.Stop();

            _resetOnNextUserMove = true;

            TriggerVictory();
        }

        if (_scrambleTurns > 0)
        {
            _scrambleTurns--;

            if (_scrambleTurns < 2)
            {
                _rotationDuration = RotationDuration;
            }

            if (_scrambleTurns == 0)
            {
                _isScrambling = false;
            }
        }

        if (_isSolving && _solverFinished && _solveQueue.Count == 0)
        {
            _isSolving = false;

            _rotationDuration = RotationDuration;
        }

        if (! _isSolving && _cube.MoveCount > 0)
        {
            _logger?.WriteLine($"Move count: {_cube.MoveCount}.");
        }

        _isUndoRedo = false;

        _isUndo = false;
    }

    private void TriggerVictory()
    {
        _victoryActive = true;

        _victoryTime = 0f;

        _victoryYaw = _yaw;

        _victoryPitch = _pitch;

        _solvedSound.Play();

        _solverStage = null;
    }

    private void DrawQube()
    {
        foreach (var cubie in _cubies)
        {
            _primitiveTransform = GetCubieAnimationTransform(cubie);

            DrawCubie(cubie);
        }

        _primitiveTransform = Matrix.Identity;
    }

    private Matrix GetCubieAnimationTransform(Cubie cubie)
    {
        if (_activeRotation is not { } rotation || ! IsCubieOnFace(cubie, rotation.Face))
        {
            return Matrix.Identity;
        }

        var progress = MathHelper.Clamp(rotation.Elapsed / _rotationDuration, 0f, 1f);

        var easedProgress = 1f - MathF.Pow(1f - progress, 3f);

        return CreateTurnMatrix(rotation.Face, rotation.Direction, easedProgress * QuarterTurn);
    }

    private Matrix CreateTurnMatrix(Face face, Direction direction, float angle)
    {
        var outwardNormal = NormalForFace(face);

        var signedAngle = direction switch
        {
            Direction.Clockwise => -angle,
            Direction.AntiClockwise => angle,
            Direction.HalfTurn => _isUndo ? angle * 2 : -angle * 2,
            _ => 0
        };

        signedAngle *= AxisSign(outwardNormal);

        return Matrix.CreateFromAxisAngle(AbsAxis(outwardNormal), signedAngle);
    }

    private static bool IsCubieOnFace(Cubie cubie, Face face)
    {
        return IsCubieOnFace(cubie.Position, face);
    }

    private static bool IsCubieOnFace(Vector3 position, Face face)
    {
        return face switch
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            Face.Up => position.Y == 1,
            Face.Down => position.Y == -1,
            Face.Front => position.Z == 1,
            Face.Back => position.Z == -1,
            Face.Left => position.X == -1,
            Face.Right => position.X == 1,
            // ReSharper restore CompareOfFloatsByEqualityOperator
            _ => false
        };
    }

    private static Vector3 NormalForFace(Face face)
    {
        return face switch
        {
            Face.Up => Vector3.Up,
            Face.Down => Vector3.Down,
            Face.Front => new Vector3(0, 0, 1),
            Face.Back => new Vector3(0, 0, -1),
            Face.Left => Vector3.Left,
            Face.Right => Vector3.Right,
            _ => Vector3.Zero
        };
    }

    private static Face FaceFromNormal(Vector3 normal)
    {
        if (normal == Vector3.Up)
        {
            return Face.Up;
        }

        if (normal == Vector3.Down)
        {
            return Face.Down;
        }

        if (normal == new Vector3(0, 0, 1))
        {
            return Face.Front;
        }

        if (normal == new Vector3(0, 0, -1))
        {
            return Face.Back;
        }

        if (normal == Vector3.Left)
        {
            return Face.Left;
        }

        return Face.Right;
    }

    private static Vector3 AbsAxis(Vector3 normal)
    {
        return new Vector3(MathF.Abs(normal.X), MathF.Abs(normal.Y), MathF.Abs(normal.Z));
    }

    private static float AxisSign(Vector3 normal)
    {
        return normal.X + normal.Y + normal.Z;
    }

    private static Vector3 RoundToGrid(Vector3 value)
    {
        return new Vector3(MathF.Round(value.X), MathF.Round(value.Y), MathF.Round(value.Z));
    }

    private void DrawBox(Vector3 c, float h, Color color)
    {
        DrawBox(c, h, h, h, color);
    }

    private void DrawCubie(Cubie cubie)
    {
        var centre = cubie.Position * (_cubeSpacing + +_victorySpacingOffset);

        const float half = CubieSize / 2f;

        DrawBox(centre, half, Color.Black);

        const float stickerHalf = half - StickerInset;

        foreach (var sticker in cubie.Stickers)
        {
            DrawSticker(
                centre + sticker.Normal * (half + StickerOffset),
                sticker.Face,
                stickerHalf,
                StickerThickness,
                sticker.Color);
        }
    }

    private void DrawBox(Vector3 c, float hx, float hy, float hz, Color color)
    {
        DrawQuad(
            new Vector3(c.X - hx, c.Y + hy, c.Z - hz),
            new Vector3(c.X + hx, c.Y + hy, c.Z - hz),
            new Vector3(c.X + hx, c.Y + hy, c.Z + hz),
            new Vector3(c.X - hx, c.Y + hy, c.Z + hz),
            color);

        DrawQuad(
            new Vector3(c.X - hx, c.Y - hy, c.Z + hz),
            new Vector3(c.X + hx, c.Y - hy, c.Z + hz),
            new Vector3(c.X + hx, c.Y - hy, c.Z - hz),
            new Vector3(c.X - hx, c.Y - hy, c.Z - hz),
            color);

        DrawQuad(
            new Vector3(c.X - hx, c.Y - hy, c.Z + hz),
            new Vector3(c.X + hx, c.Y - hy, c.Z + hz),
            new Vector3(c.X + hx, c.Y + hy, c.Z + hz),
            new Vector3(c.X - hx, c.Y + hy, c.Z + hz),
            color);

        DrawQuad(
            new Vector3(c.X + hx, c.Y - hy, c.Z - hz),
            new Vector3(c.X - hx, c.Y - hy, c.Z - hz),
            new Vector3(c.X - hx, c.Y + hy, c.Z - hz),
            new Vector3(c.X + hx, c.Y + hy, c.Z - hz),
            color);

        DrawQuad(
            new Vector3(c.X - hx, c.Y - hy, c.Z - hz),
            new Vector3(c.X - hx, c.Y - hy, c.Z + hz),
            new Vector3(c.X - hx, c.Y + hy, c.Z + hz),
            new Vector3(c.X - hx, c.Y + hy, c.Z - hz),
            color);

        DrawQuad(
            new Vector3(c.X + hx, c.Y - hy, c.Z + hz),
            new Vector3(c.X + hx, c.Y - hy, c.Z - hz),
            new Vector3(c.X + hx, c.Y + hy, c.Z - hz),
            new Vector3(c.X + hx, c.Y + hy, c.Z + hz),
            color);
    }

    private void DrawSticker(Vector3 c, Face face, float half, float thickness, Color color)
    {
        var t = thickness / 2f;

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (face)
        {
            case Face.Up:
                DrawBox(c + new Vector3(0, t, 0), half, t, half, color);
                break;

            case Face.Down:
                DrawBox(c + new Vector3(0, -t, 0), half, t, half, color);
                break;

            case Face.Front:
                DrawBox(c + new Vector3(0, 0, t), half, half, t, color);
                break;

            case Face.Back:
                DrawBox(c + new Vector3(0, 0, -t), half, half, t, color);
                break;

            case Face.Left:
                DrawBox(c + new Vector3(-t, 0, 0), t, half, half, color);
                break;

            case Face.Right:
                DrawBox(c + new Vector3(t, 0, 0), t, half, half, color);
                break;
        }
    }

    private void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
    {
        var vertices = new[]
        {
            new VertexPositionColor(Vector3.Transform(a, _primitiveTransform), color),
            new VertexPositionColor(Vector3.Transform(b, _primitiveTransform), color),
            new VertexPositionColor(Vector3.Transform(c, _primitiveTransform), color),
            new VertexPositionColor(Vector3.Transform(a, _primitiveTransform), color),
            new VertexPositionColor(Vector3.Transform(c, _primitiveTransform), color),
            new VertexPositionColor(Vector3.Transform(d, _primitiveTransform), color)
        };

        GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
    }
}