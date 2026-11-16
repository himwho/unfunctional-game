using UnityEngine;
using UnityEngine.UI;

/// <summary>Physical keypad aim offsets are authored against a 1080px-high game view.</summary>
public sealed class PhysicalKeypadAim : MonoBehaviour
{
    Canvas canvas;
    Image marker;
    int updatedFrame=-1;
    public static Vector3 ScreenPoint(Camera camera,float x,float y)
    {
        Rect rect=camera!=null?camera.pixelRect:new Rect(0,0,Screen.width,Screen.height);
        float scale=rect.height/1080f;
        return new Vector3(rect.center.x+x*scale,rect.center.y+y*scale,0);
    }
    public static void ShowTarget(LevelManager level,Transform keypad,Vector3 point,WorldKeypadButton target)
    {
        var view=level.GetComponent<PhysicalKeypadAim>();
        if(view==null)view=level.gameObject.AddComponent<PhysicalKeypadAim>();
        if(view.canvas==null)
        {
            view.canvas=GameScreenUI.Canvas(level.transform,"PhysicalKeypadAim",21);
            view.marker=GameScreenUI.Panel(view.canvas.transform,"FingerAim",Color.yellow,Vector2.zero,Vector2.zero);
            view.marker.rectTransform.sizeDelta=new Vector2(9,9);view.marker.raycastTarget=false;
            Object.Destroy(view.canvas.GetComponent<GraphicRaycaster>());
        }
        view.updatedFrame=Time.frameCount;
        view.marker.rectTransform.anchorMin=view.marker.rectTransform.anchorMax=new Vector2(point.x/Screen.width,point.y/Screen.height);
        view.marker.rectTransform.anchoredPosition=Vector2.zero;
        view.marker.color=target!=null?new Color(.3f,1,.4f):new Color(1,.8f,.3f,.65f);
        bool near=keypad!=null && Camera.main!=null && Vector3.Distance(Camera.main.transform.position,keypad.position)<4;
        view.canvas.gameObject.SetActive(near && Cursor.lockState==CursorLockMode.Locked);
    }
    void LateUpdate()
    {
        if(canvas!=null && (updatedFrame!=Time.frameCount || GameManager.Instance!=null &&
            (GameManager.Instance.IsLoading || GameManager.Instance.CurrentState!=GameManager.GameState.Playing)))canvas.gameObject.SetActive(false);
    }
}
