# Rubik's Cube Simulator and Solver

## Controls

- Use the arrow keys or drag empty space with the left mouse button to orbit the cube.
- Use the mouse wheel to zoom in or out.
- Click a cube face, or press `U`, `D`, `F`, `B`, `L`, or `R`, to animate a clockwise quarter-turn of that face.
- Hold `Shift` while clicking a face or pressing a face key to animate the counter-clockwise turn.
- Press `Space` to search for and animate a solution from the current cube state.
- Press `Esc` to exit.
- Press `S` to scramble.

## Notes

### Faces

White  | Up
Yellow | Down
Red    | Front
Orange | Back
Blue   | Left
Green  | Right

### Net Layout

```
    +---+
    | U |
+---+---+---+---+
| L | F | R | B |
+---+---+---+---+
    | D |
    +---+
```

### Coordinates

```
                            U

                    (0,0) (1,0) (2,0)
                    (0,1) (1,1) (2,1)
                    (0,2) (1,2) (2,2)

        L                   F                   R                   B

(0,0) (1,0) (2,0)   (0,0) (1,0) (2,0)   (0,0) (1,0) (2,0)   (0,0) (1,0) (2,0)
(0,1) (1,1) (2,1)   (0,1) (1,1) (2,1)   (0,1) (1,1) (2,1)   (0,1) (1,1) (2,1)
(0,2) (1,2) (2,2)   (0,2) (1,2) (2,2)   (0,2) (1,2) (2,2)   (0,2) (1,2) (2,2)

                            D

                    (0,0) (1,0) (2,0)
                    (0,1) (1,1) (2,1)
                    (0,2) (1,2) (2,2)
```

