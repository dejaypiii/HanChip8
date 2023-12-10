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

    public void LoadProgram(byte[] program)
    {
        throw new NotImplementedException();
    }

    public void Run()
    {
        while (true) // TODO run at 60fps
        {
            if (DelayTimer > 0)
            {
                DelayTimer--;
                throw new NotImplementedException("Handle delay timer");
            }

            if(SoundTimer > 0)
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
            byte z = (byte)(instructionLow & 0x0F);
            byte kk = instructionLow;
            ushort nnn = (ushort)((x << 8) & kk);
            AdvanceToNextInstruction();
            
            switch (opCode, x, y, z)
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
                case (0x2, _, _, _ ): // CALL addr
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
                case (0x04, _, _, _): // SNE Vx, byte
                    {
                        if (V[x] != kk)
                        {
                            AdvanceToNextInstruction();
                        }
                        break;
                    }
                case (0x05, _, _, 0x0): // SE Vx, Vy
                    {
                        if (V[x] == V[y])
                        {
                            AdvanceToNextInstruction();
                        }
                        break;
                    }
                case (0x06, _, _, _): // LD Vx, byte
                    {
                        V[x] = kk;
                        break;
                    }
                case (0x07, _, _, _): // ADD Vx, byte
                    {
                        V[x] = (byte)(V[x] + kk);
                        break;
                    }
                case (0x08, _, _, 0x0): // LD Vx, Vy
                    {
                        V[x] = V[y];
                        break;
                    }
                case (0x08, _, _, 0x1): // OR Vx, Vy
                    {
                        V[x] = (byte)(V[x] | V[y]);
                        break;
                    }
                case (0x08, _, _, 0x2): // AND Vx, Vy
                    {
                        V[x] = (byte)(V[x] & V[y]);
                        break;
                    }
                case (0x08, _, _, 0x3): // XOR Vx, Vy
                    {
                        V[x] = (byte)(V[x] ^ V[y]);
                        break;
                    }
                case (0x08, _, _, 0x4): // ADD Vx, Vy
                    {
                        var result = V[x] + V[y];
                        V[0xF] = (byte)(result > 0xFF ? 0x1 : 0x0);
                        V[x] = (byte)(result & 0xFF);
                        break;
                    }
                case (0x08, _, _, 0x5): // SUB Vx, Vy
                    {
                        V[0xF] = (byte)(V[x] > V[y] ? 0x1 : 0x0);
                        V[x] = (byte)(V[x] - V[y]);
                        break;
                    }
                case (0x8, _, _, 0x6): // SHR Vx {, Vy}
                    {
                        V[0xF] = (byte)((V[x] & 0x1) == 0x1 ? 0x1 : 0x0);
                        V[x] >>= 1;
                        break;
                    }


/* TODO
 * 
8xy7 - SUBN Vx, Vy
Set Vx = Vy - Vx, set VF = NOT borrow.

If Vy > Vx, then VF is set to 1, otherwise 0. Then Vx is subtracted from Vy, and the results stored in Vx.


8xyE - SHL Vx {, Vy}
Set Vx = Vx SHL 1.

If the most-significant bit of Vx is 1, then VF is set to 1, otherwise to 0. Then Vx is multiplied by 2.


9xy0 - SNE Vx, Vy
Skip next instruction if Vx != Vy.

The values of Vx and Vy are compared, and if they are not equal, the program counter is increased by 2.


Annn - LD I, addr
Set I = nnn.

The value of register I is set to nnn.


Bnnn - JP V0, addr
Jump to location nnn + V0.

The program counter is set to nnn plus the value of V0.


Cxkk - RND Vx, byte
Set Vx = random byte AND kk.

The interpreter generates a random number from 0 to 255, which is then ANDed with the value kk. The results are stored in Vx. See instruction 8xy2 for more information on AND.


Dxyn - DRW Vx, Vy, nibble
Display n-byte sprite starting at memory location I at (Vx, Vy), set VF = collision.

The interpreter reads n bytes from memory, starting at the address stored in I. These bytes are then displayed as sprites on screen at coordinates (Vx, Vy). Sprites are XORed onto the existing screen. If this causes any pixels to be erased, VF is set to 1, otherwise it is set to 0. If the sprite is positioned so part of it is outside the coordinates of the display, it wraps around to the opposite side of the screen. See instruction 8xy3 for more information on XOR, and section 2.4, Display, for more information on the Chip-8 screen and sprites.


Ex9E - SKP Vx
Skip next instruction if key with the value of Vx is pressed.

Checks the keyboard, and if the key corresponding to the value of Vx is currently in the down position, PC is increased by 2.


ExA1 - SKNP Vx
Skip next instruction if key with the value of Vx is not pressed.

Checks the keyboard, and if the key corresponding to the value of Vx is currently in the up position, PC is increased by 2.


Fx07 - LD Vx, DT
Set Vx = delay timer value.

The value of DT is placed into Vx.


Fx0A - LD Vx, K
Wait for a key press, store the value of the key in Vx.

All execution stops until a key is pressed, then the value of that key is stored in Vx.


Fx15 - LD DT, Vx
Set delay timer = Vx.

DT is set equal to the value of Vx.


Fx18 - LD ST, Vx
Set sound timer = Vx.

ST is set equal to the value of Vx.


Fx1E - ADD I, Vx
Set I = I + Vx.

The values of I and Vx are added, and the results are stored in I.


Fx29 - LD F, Vx
Set I = location of sprite for digit Vx.

The value of I is set to the location for the hexadecimal sprite corresponding to the value of Vx. See section 2.4, Display, for more information on the Chip-8 hexadecimal font.


Fx33 - LD B, Vx
Store BCD representation of Vx in memory locations I, I+1, and I+2.

The interpreter takes the decimal value of Vx, and places the hundreds digit in memory at location in I, the tens digit at location I+1, and the ones digit at location I+2.


Fx55 - LD [I], Vx
Store registers V0 through Vx in memory starting at location I.

The interpreter copies the values of registers V0 through Vx into memory, starting at the address in I.


Fx65 - LD Vx, [I]
Read registers V0 through Vx from memory starting at location I.

The interpreter reads values from memory starting at location I into registers V0 through Vx.
*/
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

public interface ISoundDevice {
    void On();
    void Off();

}
public sealed class SoundLogger: ISoundDevice {
    public void On() => Console.WriteLine("Sound device on");
    public void Off() => Console.WriteLine("Sound device off");
}

public sealed class Keyboard
{
    /// <summary>
    /// Mapping from PC input to Chip8 input
    /// </summary>
    public Dictionary<char, byte> KeyMapping { get; private init; } = new()
    {
        ['6'] = 0x1, ['7'] = 0x2, ['8'] = 0x3, ['9'] = 0xC, 
        ['z'] = 0x4, ['u'] = 0x5, ['i'] = 0x6, ['o'] = 0xD, 
        ['h'] = 0x7, ['j'] = 0x8, ['k'] = 0x0, ['l'] = 0xE, 
        ['n'] = 0xA, ['m'] = 0x0, [','] = 0xB, ['.'] = 0xF, 
    };
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
        [0x0] = new byte[5,8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x1] =  new byte[5,8] {
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 1, 1, 0, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x2] =  new byte[5,8] {
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
        [0x4] =  new byte[5,8] {
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
        },
        [0x5] =  new byte[5,8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x6] =  new byte[5,8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x7] =  new byte[5,8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 1, 0, 0, 0, 0, 0, 0},
            {0, 1, 0, 0, 0, 0, 0, 0},
        },
        [0x8] =  new byte[5,8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0x9] =  new byte[5,8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0xA] =  new byte[5,8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
        },
        [0xB] =  new byte[5,8] {
            {1, 1, 1, 0, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 0, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 0, 0, 0, 0, 0},
        },
        [0xC] =  new byte[5,8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0xD] =  new byte[5,8] {
            {1, 1, 1, 0, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 0, 0, 1, 0, 0, 0, 0},
            {1, 1, 1, 0, 0, 0, 0, 0},
        },
        [0xE] =  new byte[5,8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
        },
        [0xF] =  new byte[5,8] {
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 1, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 0},
        }
    };
}
