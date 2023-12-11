using System.Runtime.CompilerServices;

namespace Chip8.Emulator;


/// <summary>
/// Chip8 interpreter based on http://devernay.free.fr/hacks/chip8/C8TECH10.HTM
/// </summary>
public sealed class Chip8
{
    /// <summary>
    /// - Addresses from 0x000 to 0xFFF
    /// - Location of original Interpreter 0x000 - 0x1FF (shouldn't be used by programs)
    /// - Most programs start at 0x200
    /// - ETI 660 computer programs start at 0x600
    /// </summary>
    public byte[] Memory { get; private init; } = new byte[4096];

    /// <summary>
    /// General purpose registers
    /// V0 - VF
    /// VF is used as flags by some instructions
    /// </summary>
    public byte[] V { get; private init; } = new byte[16];

    /// <summary>
    /// Stores memory addresses
    /// Usually only the lowest(rightmost) 12 bits are used.
    /// </summary>
    public ushort I { get; private set; }

    /// <summary>
    /// If not 0 than decrement at rate of 60Hz.
    /// </summary>
    public byte DelayTimer { get; private set; }

    /// <summary>
    /// If not 0 than decrement at rate of 60Hz.
    /// </summary>
    public byte SoundTimer { get; private set; }

    /// <summary>
    /// Program counter
    /// </summary>
    public ushort PC { get; private set; } = 0x200;

    /// <summary>
    /// Stack pointer
    /// </summary>
    public byte SP { get; private set; }

    public ushort[] Stack { get; private init; } = new ushort[16];

    public ISoundDevice SoundDevice { get; private init; } = new SoundLogger();
    public Display Display { get; private init; } = new();
    public IKeyboard Keyboard { get; private init; } = new FakeKeyboard();

    public void LoadProgram(byte[] program)
    {
        throw new NotImplementedException();
    }

    public void Run()
    {
        // TODO run at 60fps? if not handle timers at 60 Hz seperately
        // TODO handle input
        // TODO handle rendering
        // TODO handle sound output
        while (true)
        {
            if (DelayTimer > 0)
            {
                DelayTimer--;
            }

            if (SoundTimer > 0)
            {
                SoundDevice.On();
                SoundTimer--;
            }
            else
            {
                SoundDevice.Off();
            }

            var instructionHigh = Memory[PC];
            var instructionLow = Memory[PC + 1];
            byte opCode = (byte)(instructionHigh & 0xF0 >> 4);
            byte x = (byte)(instructionHigh & 0x0F);
            byte y = (byte)(instructionLow & 0xF0 >> 4);
            byte n = (byte)(instructionLow & 0x0F);
            byte kk = instructionLow;
            ushort nnn = (ushort)((x << 8) & kk);
            AdvanceToNextInstruction();

            switch (opCode, x, y, n)
            {
                case (0x0, 0x0, 0xE, 0x0): // CLS
                    {
                        Display.Clear();
                        break;
                    }
                case (0x0, 0x0, 0xE, 0xE): // RET
                    {
                        PC = Stack[SP];
                        SP--;
                        break;
                    }
                case (0x1, _, _, _): // JP addr
                    {
                        PC = nnn;
                        break;
                    }
                case (0x2, _, _, _): // CALL addr
                    {
                        SP++;
                        Stack[SP] = PC;
                        PC = nnn;
                        break;
                    }
                case (0x3, _, _, _): // SE Vx, byte
                    {
                        if (V[x] == kk)
                        {
                            AdvanceToNextInstruction();
                        }
                        break;
                    }
                case (0x4, _, _, _): // SNE Vx, byte
                    {
                        if (V[x] != kk)
                        {
                            AdvanceToNextInstruction();
                        }
                        break;
                    }
                case (0x5, _, _, 0x0): // SE Vx, Vy
                    {
                        if (V[x] == V[y])
                        {
                            AdvanceToNextInstruction();
                        }
                        break;
                    }
                case (0x6, _, _, _): // LD Vx, byte
                    {
                        V[x] = kk;
                        break;
                    }
                case (0x7, _, _, _): // ADD Vx, byte
                    {
                        V[x] += kk;
                        break;
                    }
                case (0x8, _, _, 0x0): // LD Vx, Vy
                    {
                        V[x] = V[y];
                        break;
                    }
                case (0x8, _, _, 0x1): // OR Vx, Vy
                    {
                        V[x] |= V[y];
                        break;
                    }
                case (0x8, _, _, 0x2): // AND Vx, Vy
                    {
                        V[x] &= V[y];
                        break;
                    }
                case (0x8, _, _, 0x3): // XOR Vx, Vy
                    {
                        V[x] ^= V[y];
                        break;
                    }
                case (0x8, _, _, 0x4): // ADD Vx, Vy
                    {
                        var result = V[x] + V[y];
                        V[0xF] = (byte)(result > 0xFF ? 0x1 : 0x0);
                        V[x] = (byte)(result & 0xFF);
                        break;
                    }
                case (0x8, _, _, 0x5): // SUB Vx, Vy
                    {
                        V[0xF] = (byte)(V[x] > V[y] ? 0x1 : 0x0);
                        V[x] -= V[y];
                        break;
                    }
                case (0x8, _, _, 0x6): // SHR Vx {, Vy}
                    {
                        V[0xF] = (byte)((V[x] & 0x1) == 0x1 ? 0x1 : 0x0);
                        V[x] >>= 1;
                        break;
                    }
                case (0x8, _, _, 0x7): // SUBN Vx, Vy 
                    {
                        V[0xF] = (byte)(V[y] > V[x] ? 0x1 : 0x0);
                        V[x] -= V[y];
                        break;
                    }
                case (0x8, _, _, 0xE): // SHL Vx {, Vy } 
                    {
                        V[0xF] = (byte)((V[x] & 0x80) == 0x80 ? 0x1 : 0x0);
                        V[x] <<= 1;
                        break;
                    }
                case (0x9, _, _, 0x0): // SNE Vx, Vy
                    {
                        if (V[x] != V[y])
                        {
                            AdvanceToNextInstruction();
                        }
                        break;
                    }
                case (0xA, _, _, _): // LD I, addr
                    {
                        I = nnn;
                        break;
                    }
                case (0xB, _, _, _): // JP V0, addr
                    {
                        PC = (ushort)(nnn + V[0]);
                        break;
                    }
                case (0xC, _, _, _): // RND Vx, byte
                    {
                        V[x] = (byte)(Random.Shared.Next(0, 256) & kk);
                        break;
                    }
                case (0xD, _, _, _): // DRW Vx, Vy, nibble
                    {
                        byte erased = 0;
                        for (var i = 0; i < n; ++i)
                        {
                            var sprite = Memory[I + i];
                            for (var pos = 0; pos < 8; ++pos)
                            {
                                var bit = sprite >> (7 - pos) & 0x1;
                                var bufferY = (y + i) % Display.Height;
                                var bufferX = (x + pos) % Display.Width;
                                if (bit == 1 && Display.Buffer[V[bufferY], V[bufferX]] == 1)
                                {
                                    erased = 1;
                                }

                                Display.Buffer[V[bufferY], V[bufferX]] = (byte)(Display.Buffer[V[bufferY], V[bufferX]] ^ bit);
                            }

                        }
                        V[0xF] = erased;
                        break;
                    }
                case (0xE, _, 0x9, 0xE): // SKP Vx
                    {
                        if (Keyboard.KeyPositions[V[x]] == KeyPosition.Down)
                        {
                            AdvanceToNextInstruction();
                        }
                        break;
                    }
                case (0xE, _, 0xA, 0x1): // SKNP Vx
                    {
                        if (Keyboard.KeyPositions[V[x]] == KeyPosition.Up)
                        {
                            AdvanceToNextInstruction();
                        }
                        break;
                    }
                case (0xF, _, 0x0, 0x7): // LD Vx, DT
                    {
                        V[x] = DelayTimer;
                        break;
                    }
                case (0xF, _, 0x0, 0xA): // LD Vx, K
                    {
                        /* TODO Fx0A - LD Vx, K
                        Wait for a key press, store the value of the key in Vx.

                        All execution stops until a key is pressed, then the value of that key is stored in Vx.
                        */
                        throw new NotImplementedException("LD Vx, K");
                        break;
                    }
                case (0xF, _, 0x1, 0x5): // LD DT, Vx
                    {
                        DelayTimer = V[x];
                        break;
                    }
                case (0xF, _, 0x1, 0x8): // LD ST, Vx
                    {
                        SoundTimer = V[x];
                        break;
                    }
                case (0xF, _, 0x1, 0xE): // ADD I, Vx
                    {
                        I += V[x];
                        break;
                    }
                case (0xF, _, 0x2, 0x9): // LD F, Vx
                    {
                        /* TODO Fx29 - LD F, Vx
Set I = location of sprite for digit Vx.

The value of I is set to the location for the hexadecimal sprite corresponding to the value of Vx. See section 2.4, Display, for more information on the Chip-8 hexadecimal font.
                        */
                        throw new NotImplementedException("LD F, Vx");
                        break;
                    }
                case (0xF, _, 0x3, 0x3): // LD B, Vx
                    {
                        Memory[I] = (byte)(V[x] / 100 % 10);
                        Memory[I + 1] = (byte)(V[x] / 10 % 10);
                        Memory[I + 2] = (byte)(V[x] % 10);
                        break;
                    }
                case (0xF, _, 0x5, 0x5): // LD [I], Vx
                    {
                        for (var i = 0; i <= x; ++i)
                        {
                            Memory[I + i] = V[i];
                        }
                        break;
                    }
                case (0xF, _, 0x6, 0x5):
                    {
                        for (var i = 0; i <= x; ++i)
                        {
                            V[i] = Memory[I + i];
                        }
                        break;
                    }
                default:
                    Console.WriteLine($"Ignored instruction: {instructionHigh:X}{instructionLow:X}");
                    break;
            }
            throw new NotImplementedException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AdvanceToNextInstruction() => PC += 2;
}

public interface ISoundDevice
{
    void On();
    void Off();

}
public sealed class SoundLogger : ISoundDevice
{
    public void On() => Console.WriteLine("Sound device on");
    public void Off() => Console.WriteLine("Sound device off");
}

public interface IKeyboard
{
    public Dictionary<byte, KeyPosition> KeyPositions { get; }
}

public sealed class FakeKeyboard : IKeyboard
{
    public Dictionary<byte, KeyPosition> KeyPositions { get; } = new Dictionary<byte, KeyPosition>
    {
        [0x0] = KeyPosition.Up,
        [0x1] = KeyPosition.Up,
        [0x2] = KeyPosition.Up,
        [0x3] = KeyPosition.Up,
        [0x4] = KeyPosition.Up,
        [0x5] = KeyPosition.Up,
        [0x6] = KeyPosition.Up,
        [0x7] = KeyPosition.Up,
        [0x8] = KeyPosition.Up,
        [0x9] = KeyPosition.Up,
        [0xA] = KeyPosition.Up,
        [0xB] = KeyPosition.Up,
        [0xC] = KeyPosition.Up,
        [0xD] = KeyPosition.Up,
        [0xE] = KeyPosition.Up,
        [0xF] = KeyPosition.Up,
    };

    /// <summary>
    /// Mapping from PC input to Chip8 input
    /// </summary>
    public Dictionary<char, byte> KeyMapping { get; private init; } = new()
    {
        ['6'] = 0x1,
        ['7'] = 0x2,
        ['8'] = 0x3,
        ['9'] = 0xC,
        ['z'] = 0x4,
        ['u'] = 0x5,
        ['i'] = 0x6,
        ['o'] = 0xD,
        ['h'] = 0x7,
        ['j'] = 0x8,
        ['k'] = 0x9,
        ['l'] = 0xE,
        ['n'] = 0xA,
        ['m'] = 0x0,
        [','] = 0xB,
        ['.'] = 0xF,
    };
}

public enum KeyPosition
{
    Up,
    Down
}

public sealed class Display
{
    public const int Height = 32;
    public const int Width = 64;
    /// <summary>
    /// Left top corner: (0,0) 
    /// Right top corner: (63, 0)
    /// Left bottom corner: (0, 31)
    /// Right bottom corner: (63, 31)
    /// </summary>
    public byte[,] Buffer { get; private init; } = new byte[Height, Width];

    internal void Clear()
    {
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                Buffer[y, x] = 0;
    }
}

public sealed class Sprites
{
    public Dictionary<byte, byte[,]> Fonts { get; private set; } = new()
    {
        [0x0] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x1] = new byte[5, 8] {
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 1, 1, 0, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x2] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x3] = new byte[,] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x4] = new byte[5, 8] {
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
        },
        [0x5] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x6] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x7] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 1, 0, 0, 0, 0, 0, 0},
            {0, 1, 0, 0, 0, 0, 0, 0},
        },
        [0x8] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x9] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0xA] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
        },
        [0xB] = new byte[5, 8] {
            {1, 1, 1, 0, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 0, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 0, 0, 0, 0, 0},
        },
        [0xC] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0xD] = new byte[5, 8] {
            {1, 1, 1, 0, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 0, 0, 0, 0, 0},
        },
        [0xE] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0xF] = new byte[5, 8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
        }
    };
}
