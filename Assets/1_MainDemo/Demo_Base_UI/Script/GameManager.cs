using Base.UI.Panel;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Demo
{
    public class GameManager : MonoBehaviour
    {
        private void Start()
    {
        // START GAME IN HERE
        PanelManager.Instance.CreatePanel<HomePanel>(
            panelName: "HomePanel",
            canBack: false
        ).Forget();
    }
    }
}

