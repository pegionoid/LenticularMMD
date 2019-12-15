using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


/// <summary>
/// 解像度を変更するコンポーネント
/// </summary>
[RequireComponent(typeof(Camera))]
public class ResolutionConverter : MonoBehaviour
{

    /// <summary>
    /// 解像度設定
    /// </summary>
    [SerializeField]private int OutputWidth;
    [SerializeField] private int OutputHeight;
    /// <summary>
    /// ターゲットカメラ
    /// </summary>
    private Camera m_Camera;
    /// <summary>
    /// ターゲットテクスチャ
    /// </summary>
    private RenderTexture m_TargetTexture;
    /// <summary>
    /// フレームバッファ
    /// </summary>
    private RenderTexture m_FrameBuffer;
    /// <summary>
    /// コマンドバッファ
    /// </summary>
    private CommandBuffer m_CommandBuffer;

    /// <summary>
    /// 初期化
    /// </summary>
    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
        m_TargetTexture = m_Camera.targetTexture;
    }

    /// <summary>
    /// 適用する
    /// </summary>
    public void Update()
    {
        UpdateFrameBuffer(OutputWidth, OutputHeight, 24);
        UpdateCameraTarget();
        AddCommand();
    }


    /// <summary>
    /// フレームバッファの更新
    /// </summary>
    private void UpdateFrameBuffer(int width, int height, int depth, RenderTextureFormat format = RenderTextureFormat.Default)
    {
        if (m_FrameBuffer != null)
        {
            m_FrameBuffer.Release();
            Destroy(m_FrameBuffer);
        }

        m_FrameBuffer = new RenderTexture(width, height, depth, format);
        m_FrameBuffer.useMipMap = false;
        m_FrameBuffer.Create();
    }

    /// <summary>
    /// カメラの描画先を更新
    /// </summary>
    private void UpdateCameraTarget()
    {
        if (m_FrameBuffer != null)
        {
            m_Camera.SetTargetBuffers(m_FrameBuffer.colorBuffer, m_FrameBuffer.depthBuffer);
        }
        else
        {
            m_Camera.SetTargetBuffers(Display.main.colorBuffer, Display.main.depthBuffer);
        }
    }
    
    /// <summary>
    /// コマンドを追加する
    /// </summary>
    private void AddCommand()
    {
        RemoveCommand();

        // カラーバッファをバックバッファ(画面)に描きこむコマンド
        {
            m_CommandBuffer = new CommandBuffer();
            m_CommandBuffer.name = "blit to Back buffer";

            m_CommandBuffer.SetRenderTarget(m_TargetTexture);
            m_CommandBuffer.Blit(m_FrameBuffer, BuiltinRenderTextureType.CurrentActive);

            m_Camera.AddCommandBuffer(CameraEvent.AfterEverything, m_CommandBuffer);
        }
    }
    /// <summary>
    /// コマンドを破棄する
    /// </summary>
    private void RemoveCommand()
    {
        if (m_CommandBuffer == null)
        {
            return;
        }
        if (m_Camera == null)
        {
            return;
        }

        m_Camera.RemoveCommandBuffer(CameraEvent.AfterEverything, m_CommandBuffer);
        m_CommandBuffer = null;
    }
}