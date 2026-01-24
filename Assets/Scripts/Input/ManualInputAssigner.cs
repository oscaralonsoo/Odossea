using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class ManualInputAssigner : MonoBehaviour
{
    [Header("Players")]
    public PlayerInput player1;
    public PlayerInput player2;

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Start()
    {
        // Un pequeño margen para que el sistema detecte mandos al arrancar
        Invoke(nameof(AsignarControles), 0.2f);
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        // Si se conecta, desconecta o reconecta algo, recalculamos
        if (change == InputDeviceChange.Added ||
            change == InputDeviceChange.Removed ||
            change == InputDeviceChange.Reconnected)
        {
            AsignarControles();
        }
    }

    public void AsignarControles()
    {
        if (player1 == null || player2 == null) return;

        var gamepads = Gamepad.all;
        var keyboard = Keyboard.current;

        ClearUser(player1);
        ClearUser(player2);

        // --- CONFIGURACIÓN JUGADOR 1 (Híbrida) ---
        // Siempre le damos el teclado para que WASD funcione
        if (keyboard != null)
        {
            PairDevice(player1, keyboard);
        }

        // Si hay al menos un mando, también se lo damos al P1
        if (gamepads.Count > 0)
        {
            PairDevice(player1, gamepads[0]);
            // Usamos el esquema de Gamepad porque suele ser el más completo
            ActivateScheme(player1, "Gamepad");
        }
        else
        {
            ActivateScheme(player1, "Teclado 1");
        }

        // --- CONFIGURACIÓN JUGADOR 2 ---
        if (gamepads.Count > 1)
        {
            PairDevice(player2, gamepads[1]);
            ActivateScheme(player2, "Gamepad");
        }
        else if (keyboard != null)
        {
            PairDevice(player2, keyboard);
            ActivateScheme(player2, "Teclado 2");
        }

        player1.neverAutoSwitchControlSchemes = true;
        player2.neverAutoSwitchControlSchemes = true;
    }

    private void ClearUser(PlayerInput player)
    {
        if (player.user.valid)
            player.user.UnpairDevices();
    }

    private void PairDevice(PlayerInput player, InputDevice device)
    {
        if (player.user.valid && device != null)
            InputUser.PerformPairingWithDevice(device, player.user);
    }

    private void ActivateScheme(PlayerInput player, string scheme)
    {
        if (player.user.valid)
            player.user.ActivateControlScheme(scheme);
    }
}