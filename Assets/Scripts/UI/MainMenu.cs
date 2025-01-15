using UnityEngine;

namespace ToyTanks
{
    public class MainMenu : MonoBehaviour
    {
        public void HostGame()
        {
            if (SessionManager.Instance)
            {
                SessionManager.Instance.StartHost();
            }
        }
        public void JoinGame()
        {
            if (SessionManager.Instance)
            {
                SessionManager.Instance.StartClient();
            }
        }
    }
}
