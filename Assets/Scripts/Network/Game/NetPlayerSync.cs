using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Goes on the Player1 and Player2 prefabs. Online, each PC controls ONE of them (its own) and
// shows the other one as a copy that follows the network.
//
//   local player   plays with its own physics and sends its STATE (where each of its 5 bones is)
//                  about 30 times per second.
//   remote player  is a copy: its bones become kinematic (they ignore forces) and are moved
//                  towards the last received state. They still touch things, so the copy
//                  presses switches, pushes the local player and is seen by the camera.
//
// Why state and not input: the player is a soft body made of joints and forces, so the physics
// of two PCs fed with the same input would drift apart. Sending where the bones are cannot drift.
[RequireComponent(typeof(PlayerController))]
public class NetPlayerSync : MonoBehaviour
{
    static readonly List<NetPlayerSync> s_all = new List<NetPlayerSync>();

    [Tooltip("0 = Player1 (the host plays it), 1 = Player2 (the client plays it)")]
    [SerializeField] int playerIndex;
    [SerializeField] float sendRate = 30f;
    [Tooltip("Higher = follows the network faster, lower = smoother")]
    [SerializeField] float smoothing = 25f;
    [Tooltip("Farther than this the copy jumps instead of sliding (respawn, teleport)")]
    [SerializeField] float teleportDistance = 4f;

    PlayerController m_player;
    Rigidbody2D[] m_bodies;
    bool m_remote;
    bool m_snapped;
    bool m_wasDead;
    float m_nextSend;
    ushort m_lastSequence;
    bool m_hasSequence;
    PlayerStateDTO m_target;
    bool m_hasTarget;

    public bool IsRemote { get { return m_remote; } }
    public PlayerController Player { get { return m_player; } }

    public static NetPlayerSync Find(int index)
    {
        foreach (NetPlayerSync sync in s_all)
        {
            if (sync.playerIndex == index) return sync;
        }
        return null;
    }

    void Awake()
    {
        m_player = GetComponent<PlayerController>();

        m_bodies = new Rigidbody2D[m_player.bones.Count];
        for (int i = 0; i < m_bodies.Length; i++)
            m_bodies[i] = m_player.bones[i].transform.GetComponent<Rigidbody2D>();

        m_remote = NetSession.IsOnline && NetSession.LocalIndex != playerIndex;
        if (m_remote) MakeRemoteCopy();
    }

    void OnEnable() { s_all.Add(this); }
    void OnDisable() { s_all.Remove(this); }

    void MakeRemoteCopy()
    {
        m_player.isRemote = true;

        // Nobody plays this one on this PC: its input must not read the keyboard or the gamepad.
        PlayerInput input = GetComponent<PlayerInput>();
        if (input != null) input.enabled = false;

        foreach (Rigidbody2D body in m_bodies)
        {
            if (body == null) continue;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;      // so it still triggers switches and checkpoints
        }

        // The joints keep the bones together by force: a copy that is placed by hand does not need them.
        foreach (Joint2D joint in GetComponentsInChildren<Joint2D>(true)) joint.enabled = false;
    }

    void Start()
    {
        if (!NetSession.IsOnline || m_remote) return;

        // The prefabs start with the Gamepad scheme: without a gamepad the keyboard must be used.
        PlayerInput input = GetComponent<PlayerInput>();
        if (input == null || Gamepad.current != null || Keyboard.current == null) return;

        try
        {
            if (Mouse.current != null) input.SwitchCurrentControlScheme("KeyBoard", Keyboard.current, Mouse.current);
            else input.SwitchCurrentControlScheme("KeyBoard", Keyboard.current);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[NetPlayerSync] no se pudo activar el esquema de teclado: " + e.Message);
        }
    }

    // ------------------------------------------------------------ Local player ---
    void Update()
    {
        if (!NetSession.IsOnline || m_remote) return;

        GameNet net = GameNet.Instance;
        if (net == null || Time.unscaledTime < m_nextSend) return;

        m_nextSend = Time.unscaledTime + 1f / sendRate;
        net.SendPlayerState(BuildState(net.NextSequence()));
    }

    PlayerStateDTO BuildState(ushort sequence)
    {
        int count = m_player.bones.Count;

        PlayerStateDTO dto = new PlayerStateDTO();
        dto.sequence = sequence;
        dto.playerIndex = (byte)playerIndex;
        dto.growNum = (byte)Mathf.Clamp(m_player.growNum, 0, 255);
        dto.isDead = m_player.isDead;
        dto.isGrounded = m_player.isGrounded;
        dto.verticalVelocity = m_player.AverageVerticalVelocity;
        dto.aimVisible = m_player.mira != null && m_player.mira.gameObject.activeSelf;
        dto.aimDirection = m_player.AimDirection;
        dto.bonePositions = new Vector2[count];
        dto.boneAngles = new float[count];

        for (int i = 0; i < count; i++)
        {
            Transform bone = m_player.bones[i].transform;
            dto.bonePositions[i] = bone.position;
            dto.boneAngles[i] = bone.eulerAngles.z;
        }
        return dto;
    }

    // ----------------------------------------------------------- Remote player ---
    void FixedUpdate()
    {
        if (!m_remote) return;

        GameNet net = GameNet.Instance;
        if (net == null) return;

        PlayerStateDTO state;
        if (net.TryGetPlayerState(playerIndex, out state) &&
            (!m_hasSequence || NetUtil.IsNewer(state.sequence, m_lastSequence)))
        {
            m_lastSequence = state.sequence;
            m_hasSequence = true;
            m_target = state;
            m_hasTarget = true;
        }

        if (m_hasTarget) ApplyState();
    }

    void ApplyState()
    {
        SetDead(m_target.isDead);

        if (m_player.growNum != m_target.growNum) m_player.GrowToNum(m_target.growNum);
        m_player.SetRemoteAnimation(m_target.isGrounded, m_target.verticalVelocity);
        m_player.SetRemoteAim(m_target.aimVisible, m_target.aimDirection);

        if (m_target.isDead) return;

        float t = 1f - Mathf.Exp(-smoothing * Time.fixedDeltaTime);
        int count = Mathf.Min(m_bodies.Length, m_target.bonePositions.Length);
        for (int i = 0; i < count; i++)
            MoveBone(i, m_target.bonePositions[i], m_target.boneAngles[i], t);

        m_snapped = true;
    }

    void MoveBone(int i, Vector2 position, float angle, float t)
    {
        Transform bone = m_player.bones[i].transform;
        Rigidbody2D body = m_bodies[i];

        if (body == null)
        {
            bone.position = position;
            return;
        }

        if (!m_snapped || (body.position - position).sqrMagnitude > teleportDistance * teleportDistance)
        {
            body.position = position;
            body.rotation = angle;
            return;
        }

        // MovePosition on a kinematic body gives it a velocity: what it touches is pushed correctly.
        body.MovePosition(Vector2.Lerp(body.position, position, t));
        body.MoveRotation(Mathf.LerpAngle(body.rotation, angle, t));
    }

    // The owner hides the bones while it respawns: the copy does the same.
    void SetDead(bool dead)
    {
        if (dead == m_wasDead) return;
        m_wasDead = dead;

        if (!dead)
        {
            // Put the bones where the owner respawned before showing them.
            for (int i = 0; i < m_bodies.Length && i < m_target.bonePositions.Length; i++)
                m_player.bones[i].transform.position = m_target.bonePositions[i];
        }

        m_player.isDead = dead;
        foreach (PlayerController.Bone bone in m_player.bones) bone.transform.gameObject.SetActive(!dead);
        if (!dead) m_snapped = false;       // next frame: jump to the exact position, do not slide
    }
}
