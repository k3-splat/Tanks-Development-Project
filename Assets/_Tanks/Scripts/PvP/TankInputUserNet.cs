using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Tanks.Complete
{
    /// <summary>
    /// Net版：ローカル(PhotonView.IsMine)だけが InputUser と ActionAsset を作る
    /// リモートは入力を持たない（＝誰かの入力に引っ張られない）
    /// </summary>
    public class TankInputUserNet : MonoBehaviour
    {
        public InputUser InputUser => m_InputUser;
        public InputActionAsset ActionAsset => m_LocalActionAsset;

        private PhotonView _pv;

        private InputUser m_InputUser;
        private InputActionAsset m_LocalActionAsset;

        private bool IsLocal => (_pv == null) || _pv.IsMine;

        private void Awake()
        {
            _pv = GetComponent<PhotonView>();

            // ✅ リモートは何もしない（重要）
            if (!IsLocal) return;

            // Clone the Action Map so the actions can be paired with a specific device
            m_LocalActionAsset = InputActionAsset.FromJson(InputSystem.actions.ToJson());

            // By default, pair to the keyboard
            SetNewInputUser(InputUser.PerformPairingWithDevice(Keyboard.current));
        }

        /// <summary>
        /// Activate the given control scheme on the Input User
        /// </summary>
        public void ActivateScheme(string name)
        {
            if (!IsLocal) return;
            if (!m_InputUser.valid) return;

            m_InputUser.ActivateControlScheme(name);
        }

        /// <summary>
        /// Replace the input user contained in this component by the given one
        /// </summary>
        public void SetNewInputUser(InputUser user)
        {
            if (!IsLocal) return;
            if (!user.valid) return;

            m_InputUser = user;
            m_InputUser.AssociateActionsWithUser(m_LocalActionAsset);

            if (m_InputUser.controlScheme.HasValue)
                m_InputUser.ActivateControlScheme(m_InputUser.controlScheme.Value);
        }
    }
}
