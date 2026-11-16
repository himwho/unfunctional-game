using UnityEngine;
using UnityEngine.UI;

/// <summary>Exact 4x5 row-major product atlas; sprites belong to the store scene.</summary>
public sealed class ShopProductArt : MonoBehaviour
{
    readonly Sprite[] products=new Sprite[20];
    Image detail;
    public void Build(Transform shop,Transform panel)
    {
        var atlas=Resources.Load<Texture2D>("GameArt/ShopProducts");if(atlas==null){Debug.LogError("Missing ShopProducts art atlas.");return;}
        for(int i=0;i<20;i++) {
            float w=atlas.width/4f,h=atlas.height/5f;
            products[i]=Sprite.Create(atlas,new Rect((i%4)*w,(4-i/4)*h,w,h),new Vector2(.5f,.5f),100);
            var row=Find(shop,"ItemRow_"+i);if(row==null)continue;
            AddImage(row,"ProductImage",products[i],new Vector2(.008f,.08f),new Vector2(.078f,.92f));
            foreach(string label in new[]{"Name","Rating"}) {var t=row.Find(label) as RectTransform;if(t!=null)t.anchorMin=new Vector2(.095f,t.anchorMin.y);}
        }
        detail=AddImage(panel,"ProductHero",products[0],new Vector2(.04f,.53f),new Vector2(.30f,.85f));
        var desc=panel.Find("DetailDesc") as RectTransform;if(desc!=null)desc.anchorMin=new Vector2(.34f,.55f);
    }
    static Transform Find(Transform root,string name){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
    static Image AddImage(Transform parent,string name,Sprite sprite,Vector2 min,Vector2 max)
    {
        var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(parent,false);
        GameScreenUI.Place(g.GetComponent<RectTransform>(),min,max);var im=g.GetComponent<Image>();im.sprite=sprite;im.preserveAspect=true;im.raycastTarget=false;return im;
    }
    public void Select(int index){if(detail!=null&&index>=0&&index<products.Length)detail.sprite=products[index];}
    void OnDestroy(){foreach(var s in products)if(s!=null)Destroy(s);}
}
