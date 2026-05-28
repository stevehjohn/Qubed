using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RubiksCube.Core;
using RubiksCube.Core.Infrastructure;
using RubiksCube.Core.Models;

namespace RubiksCube.FrontEnd.Display;

public sealed class RubiksCube : Game
{
    private const int NetTileSize = 20;

    private const int NetSpacing = 6;

    private const int PanelWidth = NetTileSize * 12 + NetSpacing * 13;

    private const int PanelHeight = NetTileSize * 9 + NetSpacing * 10;

    private const float CameraDistance = 9.95f;

    private const int NetLeft = 455;

    private const int NetTop = 120;

    private const float StickerInset = 0.08f;

    private const float StickerOffset = 0.015f;

    private const float StickerThickness = 0.05f;
    
    // ReSharper disable once NotAccessedField.Local
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly GraphicsDeviceManager _graphics;

    private readonly List<Cubie> _cubies = [];

    private readonly Queue<Move> _solveQueue = [];

    private readonly Random _random = new();

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

    private float _rotationDuration = 0.25f;

    private bool _solverFinished;

    private float _cubeSpacing = 9f;

    private float _cubeSpacingSpeed = 0.1f;

    private Face? _previousFace;

    private SpriteBatch _spriteBatch;

    private Texture2D _texture;

    private bool _isUndoRedo;

    private readonly Cube _cube = new();

    private readonly Color[] _data = new Color[PanelWidth * PanelHeight];

    private const float CubieSize = 1f;

    private const float CubeSpacingFinal = 1.25f;

    private const float QuarterTurn = MathHelper.PiOver2;

    private const float MouseRotationScale = 0.01f;

    private const float CubePickHalfExtent = CubeSpacingFinal + CubieSize / 2f + 0.05f;

    private readonly Lock _solveLock = new();

    private readonly Color[] _faceColors =
    [
        Color.White,
        Color.Yellow,
        Color.Red,
        Color.Orange,
        Color.Green,
        Color.Blue
    ];

    public RubiksCube()
    {
        _graphics = new GraphicsDeviceManager(this);

        IsMouseVisible = true;

        _graphics.PreferMultiSampling = true;
    }

    protected override void Initialize()
    {
        Window.Title = "Rubiks Cube";

        CreateSolvedCube();

        _previousKeyboard = Keyboard.GetState();

        _previousMouse = Mouse.GetState();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _effect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };

        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _texture = new Texture2D(GraphicsDevice, PanelWidth, PanelHeight);

        UpdateView();

        _cubeViewport = GetCubeViewport();
    }

    protected override void Update(GameTime gameTime)
    {
        if (_cubeSpacing > CubeSpacingFinal)
        {
            _cubeSpacing -= _cubeSpacingSpeed;

            _cubeSpacingSpeed += 0.01f;
        }
        else
        {
            _cubeSpacing = CubeSpacingFinal;
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
        }

        if (keyboard.IsKeyDown(Keys.Right))
        {
            _yaw += 0.02f;
        }

        if (keyboard.IsKeyDown(Keys.Up))
        {
            _pitch -= 0.02f;
        }

        if (keyboard.IsKeyDown(Keys.Down))
        {
            _pitch += 0.02f;
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
            DrawRubiksCube();
        }

        GraphicsDevice.Viewport = fullViewport;

        _effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), fullViewport.AspectRatio, 0.1f, 100f);

        DrawNet();

        base.Draw(gameTime);
    }

    private void DrawNet()
    {
        _spriteBatch.Begin(SpriteSortMode.FrontToBack);

        _texture.SetData(_data);

        const int unit = NetTileSize + NetSpacing;

        DrawFace(Face.Up, NetSpacing + unit * 3, NetSpacing);

        DrawFace(Face.Left, NetSpacing, NetSpacing + unit * 3);

        DrawFace(Face.Front, NetSpacing + unit * 3, NetSpacing + unit * 3);

        DrawFace(Face.Right, NetSpacing + unit * 6, NetSpacing + unit * 3);

        DrawFace(Face.Back, NetSpacing + unit * 9, NetSpacing + unit * 3);

        DrawFace(Face.Down, NetSpacing + unit * 3, NetSpacing + unit * 6);

        _spriteBatch.Draw(_texture, new Vector2(NetLeft, NetTop), new Rectangle(0, 0, PanelWidth, PanelHeight), Color.White);

        _spriteBatch.End();
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

                _data[x + y * PanelWidth] = Color.Black;
            }
        }
    }

    private void DrawTile(Color color, int left, int top)
    {
        for (var y = 0; y < NetTileSize; y++)
        {
            for (var x = 0; x < NetTileSize; x++)
            {
                _data[left + x + (top + y) * PanelWidth] = color;
            }
        }
    }

    private void UpdateMouseControls(MouseState mouse)
    {
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

    private Viewport GetCubeViewport()
    {
        var fullViewport = GraphicsDevice.Viewport;

        return new Viewport(-40, -15, (int) (fullViewport.Width * 0.66f), fullViewport.Height);
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

        rotation.Elapsed += (float) gameTime.ElapsedGameTime.TotalSeconds;

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

        if (WasKeyPressed(keyboard, Keys.U))
        {
            StartFaceRotation(new Move(Face.Up, direction));
        }
        else if (WasKeyPressed(keyboard, Keys.D))
        {
            StartFaceRotation(new Move(Face.Down, direction));
        }
        else if (WasKeyPressed(keyboard, Keys.F))
        {
            StartFaceRotation(new Move(Face.Front, direction));
        }
        else if (WasKeyPressed(keyboard, Keys.B))
        {
            StartFaceRotation(new Move(Face.Back, direction));
        }
        else if (WasKeyPressed(keyboard, Keys.L))
        {
            StartFaceRotation(new Move(Face.Left, direction));
        }
        else if (WasKeyPressed(keyboard, Keys.R))
        {
            StartFaceRotation(new Move(Face.Right, direction));
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
            if (WasKeyPressed(keyboard, Keys.S))
            {
                _previousFace = null;

                _scrambleTurns = 20;
                
                Console.WriteLine($"Scrambling with {_scrambleTurns} moves.");
                
                _rotationDuration = 0.1f;

                _isScrambling = true;
                
                _cube.ResetMoveCount();
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
            
        } while (face == _previousFace);
        
        StartFaceRotation(new Move(face, (Direction) _random.Next(3)));
    }

    private void TryStartSolveAnimation(KeyboardState keyboard)
    {
        if (_activeRotation is not null || _isSolving || _scrambleTurns > 0 || ! WasKeyPressed(keyboard, Keys.Space))
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

        var solver = new Solver(cube, Mode.Fast, new ConsoleLogger());

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
            ? 10f / _solveQueue.Count
            : 0.25f;
        
        _rotationDuration = Math.Min(_rotationDuration, 0.25f);
    }

    private void StepCallback(List<Move> moves)
    {
        lock (_solveLock)
        {
            foreach (var move in moves)
            {
                _solveQueue.Enqueue(move);
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
        Move move;

        lock (_solveLock)
        {
            if (! _solveQueue.TryDequeue(out move))
            {
                return;
            }

            if (_solveQueue.Count < 2)
            {
                _rotationDuration = 0.25f;
            }
        }

        StartFaceRotation(move);
    }

    private void StartFaceRotation(Move move)
    {
        _activeRotation = new FaceRotation(move);
    }

    private bool WasKeyPressed(KeyboardState keyboard, Keys key)
    {
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
            _cube.ApplyMove(rotation.Face, rotation.Direction, ! _isScrambling);
        }

        if (_scrambleTurns > 0)
        {
            _scrambleTurns--;

            if (_scrambleTurns < 2)
            {
                _rotationDuration = 0.25f;
            }

            if (_scrambleTurns == 0)
            {
                _isScrambling = false;
            }
        }
        
        if (_isSolving && _solverFinished && _solveQueue.Count == 0)
        {
            _isSolving = false;

            _rotationDuration = 0.25f;
            
            _cube.ResetMoveCount();
        }

        if (! _isSolving && _cube.MoveCount > 0)
        {
            Console.WriteLine($"Move count: {_cube.MoveCount}.");
        }

        _isUndoRedo = false;
    }

    private void DrawRubiksCube()
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

    private static Matrix CreateTurnMatrix(Face face, Direction direction, float angle)
    {
        var outwardNormal = NormalForFace(face);

        var signedAngle = direction switch
        {
            Direction.Clockwise => -angle,
            Direction.AntiClockwise => angle,
            Direction.HalfTurn => angle * 2,
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
        var centre = cubie.Position * _cubeSpacing;

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