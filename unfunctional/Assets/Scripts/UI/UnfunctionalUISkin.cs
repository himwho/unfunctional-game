using UnityEngine;
using UnityEngine.UI;

/// <summary>Only graphics change: original rects, values, callbacks and raycast targets remain authored.</summary>
public static class UnfunctionalUISkin
{
    static Sprite bezel, knob;
    static Sprite Load(string name) => Resources.Load<Sprite>("GameArt/"+name);
    public static void Button(Button button)
    {
        if(button==null)return;var image=button.targetGraphic as Image;
        if(image==null)image=button.GetComponent<Image>();if(image==null)return;
        if(bezel==null)bezel=Load("ButtonBezel");
        if(bezel!=null){image.sprite=bezel;image.type=Image.Type.Sliced;}
    }
    public static void ButtonsIn(Transform parent)
    {foreach(var b in parent.GetComponentsInChildren<Button>(true))Button(b);}
    public static void Slider(Slider slider)
    {
        if(bezel==null)bezel=Load("ButtonBezel");if(knob==null)knob=Load("SliderKnob");
        var root=slider.GetComponent<Image>();if(root!=null&&bezel!=null){root.sprite=bezel;root.type=Image.Type.Sliced;}
        var handle=slider.targetGraphic as Image;if(handle!=null&&knob!=null){handle.sprite=knob;handle.type=Image.Type.Sliced;}
        var track=slider.transform.Find("Track");if(track==null||track.Find("CalibrationTicks")!=null)return;
        var ticks=new GameObject("CalibrationTicks",typeof(RectTransform));ticks.transform.SetParent(track,false);
        GameScreenUI.Place(ticks.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
        for(int i=0;i<=20;i++) {
            var mark=new GameObject("EtchedTick",typeof(RectTransform),typeof(Image));mark.transform.SetParent(ticks.transform,false);
            var rt=mark.GetComponent<RectTransform>();rt.anchorMin=new Vector2(i/20f,0);rt.anchorMax=new Vector2(i/20f,i%5==0?.48f:.25f);rt.sizeDelta=new Vector2(1,0);rt.anchoredPosition=Vector2.zero;
            var im=mark.GetComponent<Image>();im.color=new Color(.84f,.81f,.62f,.65f);im.raycastTarget=false;
        }
    }
}
