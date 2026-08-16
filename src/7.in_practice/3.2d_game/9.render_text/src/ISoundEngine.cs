/*
using System.Buffers.Binary;
using System.Text;
using NAudio.Wave;
using NLayer.NAudioSupport;
using OpenTK.Audio.OpenAL;

namespace LearnOpenTK.src;

public class ISoundEngine
{
    private ALDevice _device;
    private ALContext _context;

    private int _source;
    private int _buffer;

    public ISoundEngine()
    {
        
    }

    public void Play2D(string filePath, bool value)
    {
        // if (args.Length != 1)
        // {
        //     Console.WriteLine("Exactly one argument should be given: the path to the .wav file that should be played.");
        //     return;
        // }

        // string filePath = args[0];

        string extension = Path.GetExtension(filePath).ToLower();
        string name = Path.GetFileNameWithoutExtension(filePath);

        if (extension == ".mp3")
        {
            if (!Directory.Exists("res/audio/naudio"))
            {
                Directory.CreateDirectory("res/audio/naudio");
            }

            string infile = filePath;
            string outfile = $"res/audio/naudio/{name}.wav";

            using (var reader = new Mp3FileReaderBase(infile, wf => new Mp3FrameDecompressor(wf)))
            {
                WaveFileWriter.CreateWaveFile(outfile, reader);
            }

            filePath = outfile;
        }

        ReadOnlySpan<byte> file = File.ReadAllBytes(filePath);
        int index = 0;

        if (file[index++] != 'R' || file[index++] != 'I' || file[index++] != 'F' || file[index++] != 'F')
        {
            Console.WriteLine("O arquivo fornecido não está no formato RIFF.");
            return;
        }

        int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index,  4));
        index += 4;

        if (file[index++] != 'W' || file[index++] != 'A' || file[index++] != 'V' || file[index++] != 'E')
        {
            Console.WriteLine("O arquivo fornecido não está no formato WAVE.");
            return;
        }

        short numChannels = -1;
        int sampleRate = -1;
        int byteRate = -1;
        short blockAlign = -1;
        short bitsPerSample = -1;
        ALFormat format = 0;

        _device = ALC.OpenDevice("");
        if (_device == IntPtr.Zero)
        {
            Console.WriteLine("Não foi possível criar o dispositivo.");
            return;
        }

        _context = ALC.CreateContext(default, (int[])null!);
        ALC.MakeContextCurrent(_context);

        AL.GetError();

        _source = AL.GenSource();
        _buffer = AL.GenBuffer();
        AL.Source(_source, ALSourceb.Looping, value);

        while (index + 4 < file.Length)
        {
            string identifier = "" + (char)file[index++] + (char)file[index++] + (char)file[index++] + (char)file[index++];
            int size = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
            index += 4;

            if (identifier == "fmt ")
            {
                if (size != 16)
                {
                    Console.WriteLine($"Formato de áudio desconhecido com tamanho de subchunk1 {size}.");
                }
                else
                {
                    short audioFormat = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                    index += 2;

                    if (audioFormat != 1)
                    {
                        Console.WriteLine($"Formato de áudio desconhecido com ID {audioFormat}.");
                    }
                    else
                    {
                        numChannels = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;
                        
                        sampleRate = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                        index += 4;

                        byteRate = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(index, 4));
                        index += 4;

                        blockAlign = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;

                        bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(file.Slice(index, 2));
                        index += 2;

                        if (numChannels == 1)
                        {
                            if (bitsPerSample == 8)
                            {
                                format = ALFormat.Mono8;
                            }
                            else if (bitsPerSample == 16)
                            {
                                format = ALFormat.Mono16;
                            }
                            else
                            {
                                Console.WriteLine($"Não é possível reproduzir áudio mono de {bitsPerSample} bits por amostra.");
                            }
                        }
                        else if (numChannels == 2)
                        {
                            if (bitsPerSample == 8)
                            {
                                format = ALFormat.Stereo8;
                            }
                            else if (bitsPerSample == 16)
                            {
                                format = ALFormat.Stereo16;
                            }
                            else
                            {
                                Console.WriteLine($"Não é possível reproduzir som estéreo de {bitsPerSample} bits.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Não é possível reproduzir áudio com {numChannels} canais de som.");
                        }
                    }
                }
            }
            else if (identifier == "data")
            {
                ReadOnlySpan<byte> data = file.Slice(index, size);
                index += size;

                unsafe
                {
                    fixed(byte* pData = data)
                    {
                        AL.BufferData(_buffer, format, pData, size, sampleRate);
                    }
                }

                Console.WriteLine($"Ler {size} bytes de dados.");
            }
            else if (identifier == "JUNK")
            {
                // isso existe para alinhar as coisas
                index += size;
            }
            else if (identifier == "iXML")
            {
                ReadOnlySpan<byte> v = file.Slice(index, size);
                string str = Encoding.ASCII.GetString(v);
                Console.WriteLine($"iXML Chunk: {str}");
                index += size;
            }
            else
            {
                Console.WriteLine($"Seção desconhecida: {identifier}.");
                index += size;
            }

            Console.WriteLine($"Sucesso. Arquivo de áudio RIFF-WAVE detectado, codificação PCM. {numChannels} canais, {sampleRate} taxa de amostragem, {byteRate} taxa de bytes, {blockAlign} alinhamento de bloco, {bitsPerSample} bits por amostra.");

            AL.Source(_source, ALSourcei.Buffer, _buffer);
            AL.SourcePlay(_source);

            // Console.WriteLine("Pressione Enter para sair...");
            // Console.ReadLine();

            // AL.SourceStop(source);

            // AL.DeleteSource(source);
            // AL.DeleteBuffer(buffer);
            // ALC.DestroyContext(context);
            // ALC.CloseDevice(device);
        }
    }

    public void Drop()
    {
        AL.SourceStop(_source);

        AL.DeleteSource(_source);
        AL.DeleteBuffer(_buffer);
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
    }
}
//*/
//*
using NAudio.Wave;
using NLayer.NAudioSupport;
using OpenTK.Audio.OpenAL;

namespace LearnOpenTK.src;

public class ISoundEngine
{
    private ALDevice _device;
    private ALContext _context;
    private List<int> _sources = new List<int>();
    private List<int> _buffers = new List<int>();

    public ISoundEngine()
    {
        _device = ALC.OpenDevice("");
        if (_device == IntPtr.Zero)
        {
            Console.WriteLine("Não foi possível criar o dispositivo");
            return;
        }

        _context = ALC.CreateContext(_device, (int[])null!);
        ALC.MakeContextCurrent(_context);
        AL.GetError();
    }

    public void Play2D(string filePath, bool shouldLoop)
    {
        byte[] audioData;
        int sampleRate;
        int channels;
        
        string extension = Path.GetExtension(filePath).ToLower();
        
        if (extension == ".mp3")
        {
            // Para MP3, usa Mp3FileReaderBase
            using (var reader = new Mp3FileReaderBase(filePath, wf => new Mp3FrameDecompressor(wf)))
            {
                sampleRate = reader.Mp3WaveFormat.SampleRate;
                channels = reader.Mp3WaveFormat.Channels;
                
                Console.WriteLine($"Formato MP3: {reader.Mp3WaveFormat}");
                Console.WriteLine($"Bits por sample: {reader.Mp3WaveFormat.BitsPerSample}");
                
                // Lê os dados
                using (var memoryStream = new MemoryStream())
                {
                    reader.CopyTo(memoryStream);
                    byte[] rawData = memoryStream.ToArray();
                    
                    // Verifica o formato dos dados
                    if (reader.Mp3WaveFormat.BitsPerSample == 16)
                    {
                        // Já está em 16-bit PCM
                        audioData = rawData;
                        Console.WriteLine("Dados já em 16-bit PCM");
                    }
                    else
                    {
                        // Converte de float (32-bit) para 16-bit
                        int floatSampleCount = rawData.Length / 4;
                        audioData = new byte[floatSampleCount * 2];
                        
                        for (int i = 0; i < floatSampleCount; i++)
                        {
                            float sample = BitConverter.ToSingle(rawData, i * 4);
                            sample = Math.Clamp(sample, -1.0f, 1.0f);
                            short sample16 = (short)(sample * 32767.0f);
                            audioData[i * 2] = (byte)(sample16 & 0xFF);
                            audioData[i * 2 + 1] = (byte)((sample16 >> 8) & 0xFF);
                        }
                        Console.WriteLine("Convertido de float para 16-bit PCM");
                    }
                }
            }
        }
        else if (extension == ".wav")
        {
            // Para WAV, usa WaveFileReader
            using (var reader = new WaveFileReader(filePath))
            {
                sampleRate = reader.WaveFormat.SampleRate;
                channels = reader.WaveFormat.Channels;
                
                Console.WriteLine($"Formato WAV: {reader.WaveFormat}");
                
                // Lê os dados
                using (var memoryStream = new MemoryStream())
                {
                    reader.CopyTo(memoryStream);
                    audioData = memoryStream.ToArray();
                }
                
                // Se não for 16-bit PCM, converte
                if (reader.WaveFormat.BitsPerSample != 16 || 
                    reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                {
                    Console.WriteLine("Convertendo WAV para 16-bit PCM...");
                    
                    int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
                    int sampleCount = audioData.Length / bytesPerSample;
                    byte[] convertedData = new byte[sampleCount * 2];
                    
                    for (int i = 0; i < sampleCount; i++)
                    {
                        int offset = i * bytesPerSample;
                        float sample = 0;
                        
                        switch (reader.WaveFormat.BitsPerSample)
                        {
                            case 8:
                                // 8-bit PCM (unsigned)
                                sample = (audioData[offset] - 128) / 128.0f;
                                break;
                            case 24:
                                // 24-bit PCM
                                int value24 = audioData[offset] | 
                                             (audioData[offset + 1] << 8) | 
                                             (audioData[offset + 2] << 16);
                                if ((value24 & 0x800000) != 0)
                                    value24 |= unchecked((int)0xFF000000);
                                sample = value24 / 8388608.0f;
                                break;
                            case 32:
                                // 32-bit float ou PCM
                                sample = BitConverter.ToSingle(audioData, offset);
                                break;
                        }
                        
                        sample = Math.Clamp(sample, -1.0f, 1.0f);
                        short sample16 = (short)(sample * 32767.0f);
                        convertedData[i * 2] = (byte)(sample16 & 0xFF);
                        convertedData[i * 2 + 1] = (byte)((sample16 >> 8) & 0xFF);
                    }
                    
                    audioData = convertedData;
                }
            }
        }
        else
        {
            Console.WriteLine($"Formato não suportado: {extension}");
            return;
        }

        // Determina o formato OpenAL
        ALFormat format;
        if (channels == 1)
        {
            format = ALFormat.Mono16;
        }
        else if (channels == 2)
        {
            format = ALFormat.Stereo16;
        }
        else
        {
            Console.WriteLine($"Não é possível reproduzir áudio com {channels} canais");
            return;
        }

        // Cria source e buffer
        int source = AL.GenSource();
        int buffer = AL.GenBuffer();
        
        _sources.Add(source);
        _buffers.Add(buffer);
        
        AL.Source(source, ALSourceb.Looping, shouldLoop);

        // Carrega os dados de áudio
        unsafe
        {
            fixed (byte* pData = audioData)
            {
                AL.BufferData(buffer, format, pData, audioData.Length, sampleRate);
            }
        }

        // Verifica erros do OpenAL
        ALError error = AL.GetError();
        if (error != ALError.NoError)
        {
            Console.WriteLine($"Erro OpenAL: {error}");
        }
        
        // Reproduz o áudio
        AL.Source(source, ALSourcei.Buffer, buffer);
        AL.SourcePlay(source);
        
        Console.WriteLine($"Áudio carregado: {channels} canais, {sampleRate} Hz, {audioData.Length} bytes");
    }

    public void Drop()
    {
        foreach (var source in _sources)
        {
            AL.SourceStop(source);
            AL.DeleteSource(source);
        }
        
        foreach (var buffer in _buffers)
        {
            AL.DeleteBuffer(buffer);
        }
        
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
    }
}
//*/
