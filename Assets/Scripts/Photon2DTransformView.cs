namespace Photon.Pun
{
    using UnityEngine;
    public class Photon2DTransformView : MonoBehaviourPun, IPunObservable
    {
        private float m_Distance;
        private float m_Angle;

        private Vector2 m_Direction;
        private Vector2 m_NetworkPosition;
        private Vector2 m_StoredPosition;

        private float m_NetworkRotation;

        public bool m_SynchronizePosition = true;
        public bool m_SynchronizeRotation = true;
        public bool m_SynchronizeScale = false;

        [Tooltip("Indicates if localPosition and localRotation should be used. Scale ignores this setting, and always uses localScale to avoid issues with lossyScale.")]
        public bool m_UseLocal;

        bool m_firstTake = false;

        public void Awake()
        {
            m_StoredPosition = transform.localPosition;
            m_NetworkPosition = Vector2.zero;

            m_NetworkRotation = 0f;
        }

        private void Reset()
        {
            // Only default to true with new instances. useLocal will remain false for old projects that are updating PUN.
            m_UseLocal = true;
        }

        void OnEnable()
        {
            m_firstTake = true;
        }

        public void Update()
        {
            var tr = transform;

            if (!this.photonView.IsMine)
            {
                if (m_UseLocal)

                {
                    tr.localPosition = Vector2.MoveTowards(tr.localPosition, this.m_NetworkPosition, this.m_Distance * (1.0f / PhotonNetwork.SerializationRate));
                    tr.localRotation = Quaternion.RotateTowards(tr.localRotation, Quaternion.Euler(0,0, m_NetworkRotation), this.m_Angle * (1.0f / PhotonNetwork.SerializationRate));
                }
                else
                {
                    tr.position = Vector2.MoveTowards(tr.position, this.m_NetworkPosition, this.m_Distance * (1.0f / PhotonNetwork.SerializationRate));
                    tr.rotation = Quaternion.RotateTowards(tr.rotation, Quaternion.Euler(0, 0, m_NetworkRotation), this.m_Angle * (1.0f / PhotonNetwork.SerializationRate));
                }
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            var tr = transform;

            // Write
            if (stream.IsWriting)
            {
                if (this.m_SynchronizePosition)
                {
                    if (m_UseLocal)
                    {
                        this.m_Direction = (Vector2)tr.localPosition - this.m_StoredPosition;
                        this.m_StoredPosition = (Vector2)tr.localPosition;
                        stream.SendNext((Vector2)tr.localPosition);
                        stream.SendNext(this.m_Direction);
                    }
                    else
                    {
                        this.m_Direction = (Vector2)tr.position - this.m_StoredPosition;
                        this.m_StoredPosition = tr.position;
                        stream.SendNext((Vector2)tr.position);
                        stream.SendNext(this.m_Direction);
                    }
                }

                if (this.m_SynchronizeRotation)
                {
                    if (m_UseLocal)
                    {
                        stream.SendNext(tr.localRotation.eulerAngles.z);
                    }
                    else
                    {
                        stream.SendNext(tr.rotation.eulerAngles.z);
                    }
                }

                if (this.m_SynchronizeScale)
                {
                    stream.SendNext((Vector2)tr.localScale);
                }
            }
            // Read
            else
            {
                if (this.m_SynchronizePosition)
                {
                    this.m_NetworkPosition = (Vector2)stream.ReceiveNext();
                    this.m_Direction = (Vector2)stream.ReceiveNext();

                    if (m_firstTake)
                    {
                        if (m_UseLocal)
                            tr.localPosition = this.m_NetworkPosition;
                        else
                            tr.position = this.m_NetworkPosition;

                        this.m_Distance = 0f;
                    }
                    else
                    {
                        float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                        this.m_NetworkPosition += this.m_Direction * lag;
                        if (m_UseLocal)
                        {
                            this.m_Distance = Vector2.Distance(tr.localPosition, this.m_NetworkPosition);
                        }
                        else
                        {
                            this.m_Distance = Vector2.Distance(tr.position, this.m_NetworkPosition);
                        }
                    }

                }

                if (this.m_SynchronizeRotation)
                {
                    this.m_NetworkRotation = (float)stream.ReceiveNext();

                    if (m_firstTake)
                    {
                        this.m_Angle = 0f;

                        if (m_UseLocal)
                        {
                            tr.localRotation = Quaternion.Euler(0, 0, m_NetworkRotation);
                        }
                        else
                        {
                            tr.rotation = Quaternion.Euler(0, 0, m_NetworkRotation);
                        }
                    }
                    else
                    {
                        if (m_UseLocal)
                        {
                            this.m_Angle = Quaternion.Angle(tr.localRotation, Quaternion.Euler(0, 0, m_NetworkRotation));
                        }
                        else
                        {
                            this.m_Angle = Quaternion.Angle(tr.rotation, Quaternion.Euler(0, 0, m_NetworkRotation));
                        }
                    }
                }

                if (this.m_SynchronizeScale)
                {
                    tr.localScale = (Vector2)stream.ReceiveNext();
                }

                if (m_firstTake)
                {
                    m_firstTake = false;
                }
            }
        }
    }
}