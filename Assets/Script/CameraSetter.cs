using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 子にあるオブジェクトを円状に配置するクラス
/// </summary>
public class CameraSetter : MonoBehaviour
{

  //距離
  [SerializeField]
  private float _distance;

    //配置角度
    [SerializeField]
    private float _angle;

    //=================================================================================
    //初期化
    //=================================================================================

    private void Awake()
    {
        Deploy();
    }

    //Inspectorの内容(半径)が変更された時に実行
    private void OnValidate()
    {
        Deploy();
    }

    //子を円状に配置する(ContextMenuで鍵マークの所にメニュー追加)
    [ContextMenu("Deploy")]
    private void Deploy()
    {

        //子を取得
        List<GameObject> childList = new List<GameObject>();
        foreach (Transform child in transform)
        {
            childList.Add(child.gameObject);
        }

        //数値、アルファベット順にソート
        childList.Sort(
          (a, b) => {
              return string.Compare(a.name, b.name);
          }
        );

        ////オブジェクト間の角度差
        //float angleDiff = _angle / ((float)childList.Count-1);

        ////各オブジェクトを円状に配置
        //for (int i = 0; i < childList.Count; i++)
        //{
        //    Vector3 childPostion = new Vector3(0,0,0);
        //    Vector3 childAngle = new Vector3(0, 0, 0);

        //    float rangle = _angle / 2 - angleDiff * i;
        //    childPostion.x += _radius * Mathf.Sin(rangle * Mathf.Deg2Rad);
        //    childPostion.z += _radius * Mathf.Cos(rangle * Mathf.Deg2Rad);
        //    childAngle.y -= 180 - rangle;

        //    childList[i].transform.localPosition = childPostion;
        //    childList[i].transform.localEulerAngles = childAngle;
        //}

        //オブジェクト間の間隔
        float angleDiff = _angle / (childList.Count-1);

        //各オブジェクトを配置
        for (int i = 0; i < childList.Count; i++)
        {
            Vector3 childPostion = new Vector3(0, 0, 0);
            Vector3 childAngle = new Vector3(0, 0, 0);

            childPostion.x += _distance * Mathf.Tan((-_angle / 2 + angleDiff * i) * Mathf.Deg2Rad);
            childPostion.z += _distance;

            float rad = Mathf.Sqrt(childPostion.x * childPostion.x + childPostion.z * childPostion.z);

            childAngle.y -= 180 - Mathf.Rad2Deg * Mathf.Asin(childPostion.x / rad);

            childList[i].transform.localPosition = childPostion;
            childList[i].transform.localEulerAngles = childAngle;
        }
    }

}
