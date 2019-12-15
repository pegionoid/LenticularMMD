using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class LenticularMain : MonoBehaviour
{
    [SerializeField]
    /// <summary>
    /// 出力サイズ高さ
    /// </summary>
    private int OutputHeight;

    [SerializeField]
    /// <summary>
    /// 出力サイズ幅
    /// </summary>
    private int OutputWidth;

    [SerializeField]
    /// <summary>
    /// Pitchパラメータ
    /// </summary>
    private double Pitch;

    [SerializeField]
    /// <summary>
    /// 入力サイズ高さ
    /// </summary>
    private int InputHeight;

    [SerializeField]
    /// <summary>
    /// 入力サイズ幅
    /// </summary>
    private int InputWidth;

    [SerializeField]
    /// <summary>
    /// 入力テクスチャリスト
    /// </summary>
    private List<RenderTexture> InputTextureList = new List<RenderTexture>();
    
    private Pixel[] PixelArray;

    private int kernel;
    private uint threadSizeX, threadSizeY, threadSizeZ;
    [SerializeField]
    private ComputeShader LenticularShader;
    private ComputeBuffer PixelArrayBuffer;

    private Texture2DArray inputTexture2DArray;

    // Use this for initialization
    void Start ()
    {
        kernel = LenticularShader.FindKernel("RenderingLenticular");
        LenticularShader.GetKernelThreadGroupSizes(kernel, out threadSizeX, out threadSizeY, out threadSizeZ);
        InitializeComputeBuffer();
        LenticularShader.SetBuffer(kernel, "PixelArray", PixelArrayBuffer);

        inputTexture2DArray = new Texture2DArray(InputWidth, InputHeight, 12, UnityEngine.TextureFormat.ARGB32, mipChain: false);
    }

    // Update is called once per frame
    void Update ()
    {
            
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // 描画用テクスチャの生成
        RenderTexture Result = new RenderTexture(OutputWidth, OutputHeight, 32,RenderTextureFormat.ARGBInt);
        Result.enableRandomWrite = true;
        Result.Create();

        //===DEBUG=================================================================================================================================================
        //savePng(Result, "beforerender");
        //===DEBUG=================================================================================================================================================



        for (int i = 0; i < InputTextureList.Count; i++)
        {
            Graphics.CopyTexture(InputTextureList[i], 0, 0, 0, 0, InputWidth, InputHeight, inputTexture2DArray, i, 0, 0, 0);
            ////===DEBUG=================================================================================================================================================
            //Graphics.CopyTexture(ReadTexture("Assets / source / source"+ (i+1) + ".bmp", 600, 320), 0, inputTexture2DArray, i);
            ////===DEBUG=================================================================================================================================================
            ////===DEBUG=================================================================================================================================================
            savePng(InputTextureList[i], "source" + i + "_" + DateTime.Today.ToString("yyyyMMdd"));
            ////===DEBUG=================================================================================================================================================

        }
        LenticularShader.SetTexture(kernel, "InputTextureArray", inputTexture2DArray);
        LenticularShader.SetTexture(kernel, "Result", Result);

        LenticularShader.Dispatch(kernel
                      , OutputWidth / (int)threadSizeX
                      , OutputHeight / (int)threadSizeY
                      , (int)threadSizeZ);

        UnityEngine.Graphics.Blit(Result, dest);

        //===DEBUG=================================================================================================================================================
        savePng(Result, "afterrender_" + DateTime.Today.ToString("yyyyMMdd"));
        //===DEBUG=================================================================================================================================================

        MonoBehaviour.Destroy(Result);
    }

    

    private void OnEnable()
    {
    }

    void OnDisable()
    {
        PixelArrayBuffer.Release();
    }

    /// <summary>
    /// コンピュートバッファの初期化
    /// </summary>
    void InitializeComputeBuffer()
    {

        PixelArrayBuffer = new ComputeBuffer(OutputHeight * OutputWidth, Marshal.SizeOf(typeof(Pixel)));

        CreateLenticularPixelArray(OutputHeight, OutputWidth, InputHeight, InputWidth, Pitch);

        // バッファに適応
        PixelArrayBuffer.SetData(PixelArray);
    }


    /// <summary>
    /// 3次元ピクセル配列作成
    /// </summary>
    /// <param name="height">出力ディスプレイの縦画素数</param>
    /// <param name="width">出力ディスプレイの横画素数</param>
    /// <param name="pitch">ピッチパラメタ</param>
    ///    X|          0          |          1          |          2          |          3          |          4          |          5          |          6          |
    ///   Y |   R      G      B   |   R      G      B   |   R      G      B   |   R      G      B   |   R      G      B   |   R      G      B   |   R      G      B   |
    ///  ---+---------------------+---------------------+---------------------+---------------------+---------------------+---------------------+---------------------+
    ///   0 |( 0, 1)( 0, 3)( 0, 5)|( 0, 0)( 0, 2)( 0, 4)|( 1, 5)( 0, 1)( 0, 3)|( 1, 4)( 0, 0)( 0, 2)|( 1, 3)( 1, 5)( 0, 1)|( 1, 2)( 1, 4)( 0, 0)|( 1, 1)( 1, 3)( 1, 5)|
    ///   1 |( 0, 7)( 0, 9)( 0,11)|( 0, 6)( 0, 8)( 0,10)|( 1,11)( 0, 7)( 0, 9)|( 1,10)( 0, 6)( 0, 8)|( 1, 9)( 1,11)( 0, 7)|( 1, 8)( 1,10)( 0, 6)|( 1, 7)( 1, 9)( 1,11)|
    ///   
    ///   ↓
    ///   
    ///    X|          0          |          1          |
    ///   Y |   R      G      B   |   R      G      B   |
    ///  ---+---------------------+---------------------+
    ///   0 | 1(0,0) 3(0,0) 5(0,0)|                     |
    ///   1 | 0(0,0) 2(0,0) 4(0,0)| 6(0,0)              |
    ///   2 |        1(0,0) 3(0,0)| 5(0,0)              |
    ///   3 |        0(0,0) 2(0,0)| 4(0,0)              |
    ///   4 |               1(0,0)| 3(0,0) 5(0,0)       |
    ///   5 |               0(0,0)| 2(0,0) 4(0,0)       |
    ///   6 | 7(0,1) 9(0,1)11(0,1)|                     |
    ///   7 | 6(0,1) 8(0,1)10(0,1)| 0(1,1)              |
    ///   8 |        7(0,1) 9(0,1)|10(0,1)              |
    ///   9 |        6(0,1) 8(0,1)| 9(0,1)              |
    ///  10 |               7(0,1)| 8(0,1) 9(0,1)       |
    ///  11 |               6(0,1)| 7(0,1) 8(0,1)       |
    private void CreateLenticularPixelArray(int outputHeight, int outputWidth, int inputHeight, int inputWidth, double pitch)
    {
        // 横にPITCHの２倍のピクセルが格納される
        double wpitch = 2 * pitch;
        int picnum = Mathf.FloorToInt((float)wpitch);
        double pixelPoint = 0;
        int xbase = 0;
        int picbase = 0;
        int pic = 0;
        int wkp = 0;
        LenticularPixelPosition[][] PixelPositionArray = CreateLenticularPixelPositionArray(outputHeight, outputWidth);
        
        PixelArray = new Pixel[outputHeight * outputWidth];
        for (int i = 0; i < PixelArray.Length; i++)
        {
            PixelArray[i] = new Pixel(  new SubPixel(-1, -1, -1)
                                      , new SubPixel(-1, -1, -1)
                                      , new SubPixel(-1, -1, -1));
        }
        for (int y = 0; y < inputHeight; y++)
        {
            // 奇数行の場合、開始位置を６ずらす
            picbase = (y % 2) * 6;

            pixelPoint = 0;
            for (int x = 0; x < inputWidth; x++)
            {
                for (int k = 0; k < picnum; k++)
                {
                    if ((wkp = (int)pixelPoint + k) >= PixelPositionArray[0].Length)
                    {
                        break;
                    }
                    // 奇数行の場合、6枚目から格納する
                    pic = (picbase + k) % picnum;
                    
                    // 奇数行の1～6枚目は x = 1 から開始する
                    xbase = (picbase + k) >= picnum ? 1 : 0;

                    if (PixelPositionArray[y][wkp].R[0] != -1) PixelArray[PixelPositionArray[y][wkp].R[1] * outputWidth + PixelPositionArray[y][wkp].R[0]].R = new SubPixel(pic, x + xbase, inputHeight - (1 + y)).ToVector();
                    if (PixelPositionArray[y][wkp].G[0] != -1) PixelArray[PixelPositionArray[y][wkp].G[1] * outputWidth + PixelPositionArray[y][wkp].G[0]].G = new SubPixel(pic, x + xbase, inputHeight - (1 + y)).ToVector();
                    if (PixelPositionArray[y][wkp].B[0] != -1) PixelArray[PixelPositionArray[y][wkp].B[1] * outputWidth + PixelPositionArray[y][wkp].B[0]].B = new SubPixel(pic, x + xbase, inputHeight - (1 + y)).ToVector();
                }
                // 
                pixelPoint += wpitch;
            }
        }

        ////===DEBUG=================================================================================================================================================
        //StreamWriter sw = new StreamWriter(Application.dataPath + "/../LenticularPixelArray2.csv");
        //for (int i = 0; i < outputWidth; i++)
        //{
        //    sw.Write("\t" + i);
        //}
        //sw.Write(sw.NewLine);
        //for (int i = 0; i < outputHeight; i++)
        //{
        //    sw.Write(i);
        //    for (int j = 0; j < outputWidth; j++)
        //    {
        //        sw.Write("\t"
        //                 + PixelArray[i * outputWidth + j].R.z + "(" + PixelArray[i * outputWidth + j].R.x + "," + PixelArray[i * outputWidth + j].R.y + ")"
        //                 + PixelArray[i * outputWidth + j].G.z + "(" + PixelArray[i * outputWidth + j].G.x + "," + PixelArray[i * outputWidth + j].G.y + ")"
        //                 + PixelArray[i * outputWidth + j].B.z + "(" + PixelArray[i * outputWidth + j].B.x + "," + PixelArray[i * outputWidth + j].B.y + ")");
        //    }
        //    sw.Write(sw.NewLine);
        //}
        //sw.Close();
        //sw.Dispose();
        ////===DEBUG=================================================================================================================================================
    }

    /// <summary>
    /// 3次元ピクセル配置配列作成
    /// </summary>
    /// <param name="height">出力ディスプレイの縦画素数</param>
    /// <param name="width">出力ディスプレイの横画素数</param>
    /// <returns>3次元ピクセル配置配列</returns>
    ///   X|    0    |    1    |　　　  X|       0       |       1       |       2       |       3       |       4       |       5       |
    ///  Y | R  G  B | R  G  B |　　　 Y |  R    G    B  |  R    G    B  |  R    G    B  |  R    G    B  |  R    G    B  |  R    G    B  |
    /// ---+---------+---------+　　　---+---------------+---------------+---------------+---------------+---------------+---------------+
    ///  0 | 1  3  5 |         |　　　 0 |(0,1)(0,3)(0,5)|(0,0)(0,2)(0,4)|(1,5)(0,1)(0,3)|(1,4)(0,0)(0,2)|(1,3)(1,5)(0,1)|(1,2)(1,4)(0,0)|
    ///  1 | 0  2  4 |         |　⇒　
    ///  2 |    1  3 | 5       |
    ///  3 |    0  2 | 4       |
    ///  4 |       1 | 3  5    |
    ///  5 |       0 | 2  4    |
    private LenticularPixelPosition[][] CreateLenticularPixelPositionArray(int height, int width)
    {
        LenticularPixelPosition[][] PixelPositionArray = new LenticularPixelPosition[height/6][];

        int pixelPositionHeight = 0;
        int pixelPositionWidth = 0;
        
        for (int i = 1; i <= height; i += 6)
        {
            pixelPositionHeight = i / 6;
            PixelPositionArray[pixelPositionHeight] = new LenticularPixelPosition[width * 6];

            for (int j = 0; j < width; j++)
            {
                pixelPositionWidth = j * 6;
                PixelPositionArray[pixelPositionHeight][pixelPositionWidth] =     new LenticularPixelPosition(   new int[] { j    , height - (i + 1) }
                                                                                                               , new int[] { j    , height - (i + 3) }
                                                                                                               , new int[] { j    , height - (i + 5) });
                PixelPositionArray[pixelPositionHeight][pixelPositionWidth + 1] = new LenticularPixelPosition(   new int[] { j    , height - (i    ) }
                                                                                                               , new int[] { j    , height - (i + 2) }
                                                                                                               , new int[] { j    , height - (i + 4) });
                if(j + 1 < width)
                {
                    PixelPositionArray[pixelPositionHeight][pixelPositionWidth + 2] = new LenticularPixelPosition(new int[] { j + 1, height - (i + 5) }
                                                                                                                , new int[] { j    , height - (i + 1) }
                                                                                                                , new int[] { j    , height - (i + 3) });
                    PixelPositionArray[pixelPositionHeight][pixelPositionWidth + 3] = new LenticularPixelPosition(new int[] { j + 1, height - (i + 4) }
                                                                                                                , new int[] { j    , height - (i    ) }
                                                                                                                , new int[] { j    , height - (i + 2) });
                    PixelPositionArray[pixelPositionHeight][pixelPositionWidth + 4] = new LenticularPixelPosition(new int[] { j + 1, height - (i + 3) }
                                                                                                                , new int[] { j + 1, height - (i + 5) }
                                                                                                                , new int[] { j    , height - (i + 1) });
                    PixelPositionArray[pixelPositionHeight][pixelPositionWidth + 5] = new LenticularPixelPosition(new int[] { j + 1, height - (i + 2) }
                                                                                                                , new int[] { j + 1, height - (i + 4) }
                                                                                                                , new int[] { j    , height - (i    ) });
                }
                else
                {
                    PixelPositionArray[pixelPositionHeight][pixelPositionWidth + 2] = new LenticularPixelPosition(new int[] { -1, -1 }
                                                                                                                , new int[] { j, height - (i + 1) }
                                                                                                                , new int[] { j, height - (i + 3) });
                    PixelPositionArray[pixelPositionHeight][pixelPositionWidth + 3] = new LenticularPixelPosition(new int[] { -1, -1 }
                                                                                                                , new int[] { j, height - (i) }
                                                                                                                , new int[] { j, height - (i + 2) });
                    PixelPositionArray[pixelPositionHeight][pixelPositionWidth + 4] = new LenticularPixelPosition(new int[] { -1, -1 }
                                                                                                                , new int[] { -1, -1 }
                                                                                                                , new int[] { j, height - (i + 1) });
                    PixelPositionArray[pixelPositionHeight][pixelPositionWidth + 5] = new LenticularPixelPosition(new int[] { -1, -1 }
                                                                                                                , new int[] { -1, -1 }
                                                                                                                , new int[] { j, height - (i) });
                }
            }
        }

        //===DEBUG=================================================================================================================================================
        //StreamWriter sw = new StreamWriter(Application.dataPath + "/../LenticularPixelPositionArray.csv");
        //for (int i = 0; i < PixelPositionArray[0].Length; i++)
        //{
        //    sw.Write("\t" + i);
        //}
        //sw.Write(sw.NewLine);
        //for (int i = 0; i < PixelPositionArray.Length; i++)
        //{
        //    sw.Write(i);
        //    for (int j = 0; j < PixelPositionArray[0].Length; j++)
        //    {
        //        sw.Write("\t");
        //        try { sw.Write("(" + PixelPositionArray[i][j].R[0] + "," + PixelPositionArray[i][j].R[1] + ")"); } catch { sw.Write("(,)"); }
        //        try { sw.Write("(" + PixelPositionArray[i][j].G[0] + "," + PixelPositionArray[i][j].G[1] + ")"); } catch { sw.Write("(,)"); }
        //        try { sw.Write("(" + PixelPositionArray[i][j].B[0] + "," + PixelPositionArray[i][j].B[1] + ")"); } catch { sw.Write("(,)"); }
        //    }
        //    sw.Write(sw.NewLine);
        //}
        //sw.Close();
        //sw.Dispose();
        //===DEBUG=================================================================================================================================================

        return PixelPositionArray;
    }

    void savePng(RenderTexture renderTexture, string filename)
    {

        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        RenderTexture.active = renderTexture;
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();

        // Encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();
        UnityEngine.Object.Destroy(tex);

        //Write to a file in the project folder
        File.WriteAllBytes(Application.dataPath + "/../" + filename + ".png", bytes);

    }

    private void savePng(Texture texture, string filename)
    {
        RenderTexture rt = new RenderTexture(texture.width, texture.height, 32);
        Texture2D tex = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
        var currentRT = RenderTexture.active;
        Graphics.Blit(texture, rt);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        tex.Apply();

        // Encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();
        UnityEngine.Object.Destroy(tex);

        RenderTexture.active = currentRT;

        //Write to a file in the project folder
        File.WriteAllBytes(Application.dataPath + "/../" + filename + ".png", bytes);
    }

    byte[] ReadPngFile(string path)
    {
        FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        BinaryReader bin = new BinaryReader(fileStream);
        byte[] values = bin.ReadBytes((int)bin.BaseStream.Length);

        bin.Close();

        return values;
    }

    Texture ReadTexture(string path, int width, int height)
    {
        byte[] readBinary = ReadPngFile(path);

        Texture2D texture = new Texture2D(width, height);
        texture.LoadImage(readBinary);

        return texture;
    }

}

public struct LenticularPixelPosition
{
    public int[] R; 
    public int[] G;
    public int[] B;
    public LenticularPixelPosition(int[] R, int[] G, int[] B)
    {
        this.R = R;
        this.G = G;
        this.B = B;
    }
}

public class SubPixel
{
    public int PictureNum;
    public int X;
    public int Y;
    
    public SubPixel(int PictureNum, int X, int Y)
    {
        this.PictureNum = PictureNum;
        this.X = X;
        this.Y = Y;
    }

    public Vector3Int ToVector()
    {
        return new Vector3Int(X, Y, PictureNum);
    }
}

public struct Pixel
{
    public Vector3Int R;
    public Vector3Int G;
    public Vector3Int B;

    public Pixel(SubPixel R, SubPixel G, SubPixel B)
    {
        this.R = R.ToVector();
        this.G = G.ToVector();
        this.B = B.ToVector();
    }
}


