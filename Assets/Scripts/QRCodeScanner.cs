using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZXing;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using ZXing.Common;
using Unity.Collections;

public class QRCodeScanner : MonoBehaviour
{
    [SerializeField]
    private ARCameraManager cameraManager;
    public RawImage scanZone;
    public Button qrCodeButton;
    public TMP_InputField searchInputField;
    public TMP_Dropdown searchDropdown;
    public GameObject qrCodeOverlay;

    private bool scanning = false;
    private string apiUrl = "https://firm-polecat-neatly.ngrok-free.app/unityAR/getTargetCube.php";

    private List<string> filteredResults = new List<string>();
    public SetNavigationTarget navigationTargetHandler;

    private IBarcodeReader barcodeReader = new BarcodeReader
    {
        AutoRotate = true,
        Options = new DecodingOptions { TryHarder = true }
    };

    private void Start()
    {
        qrCodeButton.onClick.AddListener(StartScanning);
        scanZone.gameObject.SetActive(false);
        searchInputField.gameObject.SetActive(false);
        searchDropdown.gameObject.SetActive(false);
        qrCodeOverlay.SetActive(false);

        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchInputChanged);
            searchInputField.onSubmit.AddListener(OnSearchSubmit);
        }
        else
        {
            Debug.LogError("Search Input Field is not assigned.");
        }

        searchDropdown.onValueChanged.AddListener(OnDropdownSelectionChanged);
    }

    private void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void StartScanning()
    {
        scanZone.gameObject.SetActive(true);
        qrCodeOverlay.SetActive(true);
        scanning = true;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (scanning)
        {
            if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                var conversionParams = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.MirrorY
                };

                int size = image.GetConvertedDataSize(conversionParams);
                var buffer = new NativeArray<byte>(size, Allocator.Temp);
                image.Convert(conversionParams, buffer);

                Texture2D cameraImageTexture = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, conversionParams.outputFormat, false);
                cameraImageTexture.LoadRawTextureData(buffer);
                cameraImageTexture.Apply();

                buffer.Dispose();
                image.Dispose();

                var result = barcodeReader.Decode(cameraImageTexture.GetPixels32(), cameraImageTexture.width, cameraImageTexture.height);

                if (result != null && result.Text == "DEST_MENU")
                {
                    searchInputField.gameObject.SetActive(true);
                    searchDropdown.gameObject.SetActive(true);

                    scanning = false;
                    scanZone.gameObject.SetActive(false);
                    qrCodeOverlay.SetActive(false);

                    Debug.Log("QR code detected: DEST_MENU");
                }
            }
        }
    }

    public void OnSearchInputChanged(string inputText)
    {
        if (!string.IsNullOrEmpty(inputText))
        {
            StartCoroutine(FetchFilteredDestinations(inputText));
        }
        else
        {
            searchDropdown.ClearOptions();
            searchDropdown.gameObject.SetActive(false);
        }
    }

    public void OnSearchSubmit(string inputText)
    {
        if (!string.IsNullOrEmpty(inputText))
        {
            StartCoroutine(FetchFilteredDestinations(inputText));
        }
    }

    IEnumerator FetchFilteredDestinations(string query)
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "?search=" + UnityWebRequest.EscapeURL(query));
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ParseSearchResults(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error fetching destinations: " + request.error);
        }
    }

    void ParseSearchResults(string data)
    {
        filteredResults.Clear();
        string[] lines = data.Split('\n');
        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                filteredResults.Add(line.Trim());
            }
        }

        searchDropdown.ClearOptions();
        if (filteredResults.Count > 0)
        {
            searchDropdown.AddOptions(filteredResults);
            searchDropdown.gameObject.SetActive(true);
        }
        else
        {
            searchDropdown.gameObject.SetActive(false);
        }
    }

    void OnDropdownSelectionChanged(int selectedIndex)
    {
        if (selectedIndex >= 0 && selectedIndex < filteredResults.Count)
        {
            string selectedDestination = filteredResults[selectedIndex];
            searchInputField.text = selectedDestination;
            StartCoroutine(GetTargetPosition(selectedDestination));
        }
    }

    IEnumerator GetTargetPosition(string destination)
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "?destination=" + UnityWebRequest.EscapeURL(destination));
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string[] positionData = request.downloadHandler.text.Split(',');
            if (positionData.Length == 3 &&
                float.TryParse(positionData[0], out float x) &&
                float.TryParse(positionData[1], out float y) &&
                float.TryParse(positionData[2], out float z))
            {
                Vector3 targetPosition = new Vector3(x, y, z);

                if (navigationTargetHandler != null)
                {
                    navigationTargetHandler.UpdateTargetPosition(targetPosition);
                }
                else
                {
                    Debug.LogError("NavigationTargetHandler is not assigned.");
                }
            }
            else
            {
                Debug.LogError("Error parsing target position data.");
            }
        }
        else
        {
            Debug.LogError("Error fetching target position: " + request.error);
        }
    }
}
