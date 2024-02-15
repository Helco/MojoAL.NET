using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Loader;
using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Enumeration;
using Silk.NET.SDL;

namespace MojoAL.NET;

internal class MojoALLibraryNameContainer : SearchPathContainer
{
    public override string[] Windows64 => ["mojoal.dll"];
    public override string[] Windows86 => Windows64;
    public override string[] Linux => ["libmojoal.so"];
    public override string[] Android => Linux;
    public override string[] MacOS => ["libmojoal.dylib"];
    public override string[] IOS => ["__Internal"];
    public static readonly MojoALLibraryNameContainer Instance = new();
}

internal unsafe static class Sample
{
    public static void Main(string[] args)
    {
        var sdl = Sdl.GetApi();
        var ctx = new MultiNativeContext(AL.CreateDefaultContext(MojoALLibraryNameContainer.Instance.GetLibraryNames()), null);
        var al = new AL(ctx);
        ctx.Contexts[1] = new LamdaNativeContext(x => x.EndsWith("GetProcAddress") ? default : al.GetProcAddress(x));

        ctx = new MultiNativeContext(ALContext.CreateDefaultContext(MojoALLibraryNameContainer.Instance.GetLibraryNames()), null);
        var alc = new ALContext(ctx);
        ctx.Contexts[1] = new LamdaNativeContext(
            x =>
            {
                if (x.EndsWith("GetProcAddress") ||
                    x.EndsWith("GetContextsDevice") ||
                    x.EndsWith("GetCurrentContext"))
                {
                    return default;
                }

                return (nint)alc.GetProcAddress(alc.GetContextsDevice(alc.GetCurrentContext()), x);
            });

        if (!alc.TryGetExtension<Enumeration>(null, out var alEnumeration))
        {
            Console.WriteLine("No enumeration extension");
            return;
        }
        foreach (var deviceSpec in alEnumeration.GetStringList(GetEnumerationContextStringList.DeviceSpecifiers))
            Console.WriteLine(deviceSpec);
        Console.WriteLine();

        var defDeviceSpec = alEnumeration.GetString(null, GetEnumerationContextString.DefaultDeviceSpecifier);
        Console.WriteLine("Default: " + defDeviceSpec);

        var device = alc.OpenDevice(defDeviceSpec);
        if (device == null)
            throw new InvalidOperationException("Could not open default device");
        Console.WriteLine("Default: " + alc.GetContextProperty(device, GetContextString.DeviceSpecifier));
        Console.WriteLine("Extensions: " + alc.GetContextProperty(device, GetContextString.Extensions));

        //Context(al, alc, device, playFreq: 440);
        //Sine(al, alc, device);
        Wave(sdl, al, alc, device);

        float x = 0f, z = 0f;
        ConsoleKey key;
        while ((key = Console.ReadKey(true).Key) is ConsoleKey.UpArrow or ConsoleKey.DownArrow or ConsoleKey.LeftArrow or ConsoleKey.RightArrow or ConsoleKey.R)
        {
            switch (key)
            {
                case ConsoleKey.UpArrow: z -= 1f; break;
                case ConsoleKey.DownArrow: z += 1f; break;
                case ConsoleKey.LeftArrow: x += 1f; break;
                case ConsoleKey.RightArrow: x -= 1f; break;
                case ConsoleKey.R: x = z = 0f; break;
            }
            al.SetListenerProperty(ListenerVector3.Position, x, 0f, z);
        }
    }

    static void Wave(Sdl sdl, AL al, ALContext alc, Device* device)
    {
        var context = alc.CreateContext(device, null);
        if (!alc.MakeContextCurrent(context))
        {
            Console.WriteLine("Could not make context current");
            return;
        }

        var rwFromFile = (delegate* unmanaged[Cdecl]<void*, void*, RWops*>)sdl.Context.GetProcAddress("SDL_RWFromFile");
        RWops* ops;
        fixed (void* namePtr = @"C:/dev/Pack/resources/audio/speech/d001s01m1.wav"u8)
        fixed(void* modePtr = "rb"u8)
            ops = rwFromFile(namePtr, modePtr);
        AudioSpec spec = default;
        byte* audioBuf = null;
        uint audioLen = 0;
        if (sdl.LoadWAVRW(ops, 1, ref spec, ref audioBuf, ref audioLen) == null)
            throw new Exception("Could not open wave");

        var buffer = al.GenBuffer();
        al.BufferData(buffer, spec.Format switch
        {
            Sdl.AudioU8 when spec.Channels == 1 => BufferFormat.Mono8,
            Sdl.AudioU8 when spec.Channels == 2 => BufferFormat.Stereo8,
            Sdl.AudioS16Lsb when spec.Channels == 1 => BufferFormat.Mono16,
            Sdl.AudioS16Lsb when spec.Channels == 2 => BufferFormat.Stereo16,
            _ => throw new NotSupportedException("Unsupported audio spec")
        }, audioBuf, (int)audioLen, spec.Freq);

        var source = al.GenSource();
        al.SetSourceProperty(source, SourceInteger.Buffer, buffer);
        al.SetSourceProperty(source, SourceBoolean.Looping, true);
        al.SourcePlay(source);
    }

    static void Sine(AL al, ALContext alc, Device* device, int playFreq = 880)
    {

        var context = alc.CreateContext(device, null);
        if (!alc.MakeContextCurrent(context))
        {
            Console.WriteLine("Could not make context current");
            return;
        }

        int freq = 0;
        alc.GetContextProperty(device, (GetContextInteger)ContextAttributes.Frequency, 1, &freq);
        Console.WriteLine("Frequency: " + freq);

        var buffer = al.GenBuffer();
        al.BufferData(buffer, BufferFormat.Mono16, Enumerable.Range(0, 8000).Select(i => (short)(Math.Sin(i / MathF.PI / 2 * playFreq) * short.MaxValue)).ToArray(), playFreq);

        var source = al.GenSource();
        al.SetSourceProperty(source, SourceInteger.Buffer, buffer);
        al.SetSourceProperty(source, SourceBoolean.Looping, true);
        al.SourcePlay(source);

    }
}
