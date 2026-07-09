using TMPro;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using System;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] Transform roomContainer;
    [SerializeField] GameObject roomPrefab;
    [SerializeField] TMP_InputField roomNameInput;

    private List<GameObject> spawnedRooms = new();
    private GameObject lobbyRoot;
    private TMP_Text statusText;
    private Button[] lobbyButtons;
    private TMP_InputField[] lobbyInputs;
    private ISession activeSession;

    private const int MaxPlayers = 4;
    private const float RoomEntryHeight = 54f;
    private static Task servicesInitializationTask;
    private static readonly object servicesLock = new();

    void Awake()
    {
        lobbyRoot = transform.parent != null ? transform.parent.gameObject : gameObject;
        lobbyButtons = lobbyRoot.GetComponentsInChildren<Button>(true);
        lobbyInputs = lobbyRoot.GetComponentsInChildren<TMP_InputField>(true);
        EnsureRuntimeStatusText();
        ConfigureEventSystemForCurrentPlayer();
        EnsureLobbyReceivesInput();
        EnsureRoomListLayout();
        SetLobbyInteractable(true);
    }

    async void Start()
    {
        SetStatus("Conectando a Unity Services...");

        try
        {
            await EnsureServicesReady();
            SetStatus(ShouldUseHostControls() ? "Listo para crear o buscar salas." : "Player 2 listo: pulsa Refresh y unete a la sala.");
        }
        catch (Exception exception)
        {
            SetStatus(GetFriendlyError(exception));
            Debug.LogException(exception);
        }
    }

    static Task EnsureServicesReady()
    {
        lock (servicesLock)
        {
            if (servicesInitializationTask == null || servicesInitializationTask.IsFaulted || servicesInitializationTask.IsCanceled)
            {
                servicesInitializationTask = InitUnity();
            }

            return servicesInitializationTask;
        }
    }

    static async Task InitUnity()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions()
                .SetProfile(GetPlayerProfileName());

            await UnityServices.InitializeAsync(initializationOptions);
        }

        while (UnityServices.State == ServicesInitializationState.Initializing)
        {
            await Task.Yield();
        }

        if (!AuthenticationService.Instance.IsSignedIn && !AuthenticationService.Instance.IsAuthorized)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateRoom()
    {
        if (!ShouldUseHostControls())
        {
            SetStatus("Player 2 debe usar Refresh y unirse a la sala creada por Player 1.");
            return;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            HideLobby();
            return;
        }

        SetLobbyInteractable(false);
        SetStatus("Creando sala...");

        try
        {
            await EnsureServicesReady();

            string roomName = GetRoomName();
            SessionOptions options = new SessionOptions
            {
                MaxPlayers = MaxPlayers,
                IsPrivate = false,
                Name = roomName
            }.WithRelayNetwork();

            activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            Debug.Log($"Sala creada: {activeSession.Name} ({activeSession.Id})");
            SetStatus($"Sala creada: {activeSession.Name}");
            HideLobby();
        }
        catch (Exception exception)
        {
            SetLobbyInteractable(true);
            SetStatus(GetFriendlyError(exception));
            Debug.LogException(exception);
        }
    }

    public async void JoinRoom(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            SetStatus("La sala seleccionada no tiene un ID valido.");
            return;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            HideLobby();
            return;
        }

        SetLobbyInteractable(false);
        SetStatus("Uniendose a la sala...");

        try
        {
            await EnsureServicesReady();

            activeSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);

            Debug.Log($"Te uniste a la sesion {sessionId}");
            SetStatus($"Conectado a {activeSession.Name}");
            HideLobby();
        }
        catch (Exception exception)
        {
            SetLobbyInteractable(true);
            SetStatus(GetFriendlyError(exception));
            Debug.LogException(exception);
        }

    }

    public async void RefreshRooms()
    {
        SetLobbyInteractable(false);
        SetStatus("Buscando salas...");

        try
        {
            await EnsureServicesReady();
            ClearRooms();

            if (roomContainer == null)
            {
                SetStatus("No se encontro el contenedor de salas en el lobby.");
                return;
            }

            QuerySessionsResults result = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());

            foreach (ISessionInfo session in result.Sessions)
            {
                GameObject room = CreateRoomEntry(session);
                spawnedRooms.Add(room);
            }

            EnsureRoomListLayout();
            NormalizeLobbyRaycasts();
            SetStatus(result.Sessions.Count == 0 ? "No hay salas disponibles." : $"{result.Sessions.Count} sala(s) encontrada(s).");
        }
        catch (Exception exception)
        {
            SetStatus(GetFriendlyError(exception));
            Debug.LogException(exception);
        }
        finally
        {
            SetLobbyInteractable(true);
        }
    }

    void ClearRooms()
    {
        if (roomContainer == null)
        {
            spawnedRooms.Clear();
            return;
        }

        for (int i = roomContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(roomContainer.GetChild(i).gameObject);
        }

        spawnedRooms.Clear();
    }

    GameObject CreateRoomEntry(ISessionInfo session)
    {
        string sessionName = string.IsNullOrWhiteSpace(session.Name) ? "Sala sin nombre" : session.Name;
        string sessionId = session.Id;

        GameObject room = new GameObject($"JoinRoom_{sessionName}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        room.transform.SetParent(roomContainer, false);
        room.layer = lobbyRoot != null ? lobbyRoot.layer : 5;
        room.SetActive(true);

        RectTransform rectTransform = room.GetComponent<RectTransform>();
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(0f, RoomEntryHeight);

        Image image = room.GetComponent<Image>();
        image.color = new Color(0.05f, 0.11f, 0.16f, 0.94f);
        image.raycastTarget = true;

        Button button = room.GetComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => JoinRoom(sessionId));

        LayoutElement layoutElement = room.GetComponent<LayoutElement>();
        layoutElement.minHeight = RoomEntryHeight;
        layoutElement.preferredHeight = RoomEntryHeight;
        layoutElement.flexibleWidth = 1f;

        GameObject textObject = new GameObject("RoomName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(room.transform, false);
        textObject.layer = room.layer;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 5f);
        textRect.offsetMax = new Vector2(-12f, -5f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = $"Unirse a {sessionName}";
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.fontSize = 18f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 22f;
        text.raycastTarget = false;

        Debug.Log($"[Lobby] Sala listada para unirse: {sessionName} ({sessionId})");
        return room;
    }

    string GetRoomName()
    {
        string typedName = roomNameInput != null ? roomNameInput.text.Trim() : string.Empty;
        return string.IsNullOrWhiteSpace(typedName) ? $"Sala-{GetPlayerProfileName()}" : typedName;
    }

    void SetLobbyInteractable(bool interactable)
    {
        lobbyButtons = lobbyRoot.GetComponentsInChildren<Button>(true);
        lobbyInputs = lobbyRoot.GetComponentsInChildren<TMP_InputField>(true);
        bool hostControls = ShouldUseHostControls();

        foreach (Button button in lobbyButtons)
        {
            if (button != null)
            {
                button.interactable = interactable && (hostControls || !IsCreateRoomButton(button));
            }
        }

        foreach (TMP_InputField input in lobbyInputs)
        {
            if (input != null)
            {
                input.interactable = interactable && (hostControls || input != roomNameInput);
            }
        }

        NormalizeLobbyRaycasts();
    }

    bool IsCreateRoomButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        string normalizedName = NormalizeControlName(button.name);
        if (normalizedName.Contains("createroom"))
        {
            return true;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        return label != null && NormalizeControlName(label.text).Contains("createroom");
    }

    static string NormalizeControlName(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
    }

    void EnsureLobbyReceivesInput()
    {
        if (lobbyRoot == null)
        {
            return;
        }

        Canvas canvas = lobbyRoot.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;
        }

        GraphicRaycaster raycaster = lobbyRoot.GetComponentInParent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = true;
            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }

        NormalizeLobbyRaycasts();
    }

    void EnsureRoomListLayout()
    {
        if (roomContainer == null)
        {
            return;
        }

        RectTransform content = roomContainer as RectTransform;
        if (content != null)
        {
            content.localScale = Vector3.one;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, Mathf.Max(220f, roomContainer.childCount * (RoomEntryHeight + 8f) + 16f));
        }

        VerticalLayoutGroup layoutGroup = roomContainer.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = roomContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.spacing = 8f;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter fitter = roomContainer.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = roomContainer.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = roomContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    void ConfigureEventSystemForCurrentPlayer()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        }

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        eventSystem.gameObject.SetActive(true);
        eventSystem.enabled = true;

        if (!ShouldUseStandaloneUiModule())
        {
            return;
        }

        foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
        {
            if (module is StandaloneInputModule)
            {
                continue;
            }

            if (module.GetType().FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule")
            {
                module.enabled = false;
            }
        }

        StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule == null)
        {
            standaloneInputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        standaloneInputModule.enabled = true;
        standaloneInputModule.inputActionsPerSecond = 10f;
        standaloneInputModule.repeatDelay = 0.5f;

        Debug.Log("[Lobby] Main Editor Player usa StandaloneInputModule para controlar botones e input.");
    }

    void NormalizeLobbyRaycasts()
    {
        if (lobbyRoot == null)
        {
            return;
        }

        HashSet<Graphic> interactiveGraphics = new();
        foreach (Selectable selectable in lobbyRoot.GetComponentsInChildren<Selectable>(true))
        {
            if (selectable != null && selectable.targetGraphic != null)
            {
                interactiveGraphics.Add(selectable.targetGraphic);
            }
        }

        foreach (Graphic graphic in lobbyRoot.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic != null)
            {
                graphic.raycastTarget = interactiveGraphics.Contains(graphic);
            }
        }
    }

    void HideLobby()
    {
        if (lobbyRoot != null)
        {
            lobbyRoot.SetActive(false);
        }
    }

    void EnsureRuntimeStatusText()
    {
        if (statusText != null || roomNameInput == null)
        {
            return;
        }

        GameObject statusObject = new GameObject("LobbyStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        statusObject.transform.SetParent(roomNameInput.transform.parent, false);

        RectTransform rectTransform = statusObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, -135f);
        rectTransform.sizeDelta = new Vector2(520f, 44f);

        statusText = statusObject.GetComponent<TMP_Text>();
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 18f;
        statusText.color = Color.white;
        statusText.raycastTarget = false;
    }

    void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log($"[Lobby] {message}");
    }

    static string GetFriendlyError(Exception exception)
    {
        return $"No se pudo completar la operacion online: {exception.Message}";
    }

    static string GetPlayerProfileName()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-name")
            {
                return SanitizeProfile(args[i + 1]);
            }
        }

        return "Player1";
    }

    static string SanitizeProfile(string profile)
    {
        if (string.IsNullOrWhiteSpace(profile))
        {
            return "Player1";
        }

        char[] chars = profile.ToCharArray();
        int writeIndex = 0;

        for (int i = 0; i < chars.Length && writeIndex < 30; i++)
        {
            char c = chars[i];
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                chars[writeIndex] = c;
                writeIndex++;
            }
        }

        return writeIndex == 0 ? "Player1" : new string(chars, 0, writeIndex);
    }

    static bool ShouldUseStandaloneUiModule()
    {
        return ShouldUseHostControls();
    }

    static bool ShouldUseHostControls()
    {
        return string.Equals(GetPlayerProfileName(), "Player1", StringComparison.OrdinalIgnoreCase);
    }
}
