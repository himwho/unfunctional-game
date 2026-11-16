using UnityEngine;

/// <summary>Fits newly authored decorative lettering inside its physical sign.</summary>
[RequireComponent(typeof(TextMesh))]
public sealed class DepartmentLabelFit : MonoBehaviour
{
    public float width=1,height=.4f;
    void LateUpdate()
    {
        var bounds=GetComponent<Renderer>().localBounds.size;
        if(bounds.x<=0||bounds.y<=0)return;
        float scale=Mathf.Min(1,width/bounds.x,height/bounds.y);
        transform.localScale=Vector3.one*scale;enabled=false;
    }
}
