﻿using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Netherlands3D.Coordinates;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Netherlands3D.AddressSearch
{
    [RequireComponent(typeof(TMP_InputField))]
    public class AddressSearch : MonoBehaviour
    {
        private TMP_InputField searchInputField;
    
        [Tooltip("The WFS endpoint for retrieving BAG information, see: https://www.pdok.nl/geo-services/-/article/basisregistratie-adressen-en-gebouwen-ba-1")]
        [SerializeField] private string bagWfsEndpoint = "https://service.pdok.nl/lv/bag/wfs/v2_0";
        
        [Tooltip("The endpoint for retrieving suggestions when looking up addresses, see: https://www.pdok.nl/restful-api/-/article/pdok-locatieserver-1")]
        [SerializeField] private string locationSuggestionEndpoint = "https://api.pdok.nl/bzk/locatieserver/search/v3_1/suggest";
        
        [Tooltip("The endpoint for looking up addresses, see: https://www.pdok.nl/restful-api/-/article/pdok-locatieserver-1")]
        [SerializeField] private string locationLookupEndpoint = "https://api.pdok.nl/bzk/locatieserver/search/v3_1/lookup";
        
        [SerializeField] private string searchWithinCity = "Amsterdam"; 

        [Tooltip("The type of address to filter on, see: https://www.pdok.nl/restful-api/-/article/pdok-locatieserver-1")]
        [SerializeField] private string typeFilter = ""; 
        [SerializeField] private int rows = 5; 
        [SerializeField] private int charactersNeededBeforeSearch = 2;       
        [SerializeField] private GameObject resultsParent;
        [SerializeField] private Button suggestionPrefab;

        [Header("Camera Controls")]   
        [SerializeField] private bool moveCamera = true;
        [SerializeField] private bool easeCamera = false;
        [SerializeField] private Camera mainCamera; 
        [SerializeField] private Quaternion targetCameraRotation = Quaternion.Euler(45, 0, 0);
        [SerializeField] public AnimationCurve cameraMoveCurve;

        public UnityEvent<bool> onClearButtonToggled = new();
        public UnityEvent<Coordinate> onCoordinateFound = new();
        public UnityEvent<List<string>> onSelectedBuildings = new();

        public string SearchInput { get => searchInputField.text; set => searchInputField.text = Regex.Replace(value, "<.*?>", string.Empty); }

        private const string REPLACEMENT_STRING = "{SEARCHTERM}";

        public bool IsFocused => searchInputField.isFocused;

        private void Start()
        {
            if (!mainCamera) mainCamera = Camera.main;

            searchInputField = GetComponent<TMP_InputField>();
            searchInputField.onValueChanged.AddListener(delegate { GetSuggestions(searchInputField.text); });
        }

        public void ClearInput()
        {
            ClearSearchResults();
            searchInputField.text = "";
            GetSuggestions(searchInputField.text);
            searchInputField.Select();
        }

        public void GetSuggestions(string textInput = "")
        {
            StopAllCoroutines();

            var isEmpty = textInput.Trim() == "";
            if (isEmpty)
            {
                ClearSearchResults();
                onClearButtonToggled.Invoke(false);
                return;
            }

            if (textInput.Length <= charactersNeededBeforeSearch)
            {
                ClearSearchResults();
                return;
            }

            onClearButtonToggled.Invoke(true);

            StartCoroutine(FindSearchSuggestions(textInput));
        }

        IEnumerator FindSearchSuggestions(string searchTerm)
        {
            string urlEncodedSearchTerm = UnityWebRequest.EscapeURL(searchTerm);

            var searchWithinQuery = (searchWithinCity.Length>0) ? $"and%20{searchWithinCity}%20" : "";
            var filterTypeQuery = (typeFilter.Length>0) ? $"and%20type:{typeFilter}" : "";
            string url =$"{locationSuggestionEndpoint}?q={urlEncodedSearchTerm}%20{searchWithinQuery}{filterTypeQuery}&rows={rows}".Replace(REPLACEMENT_STRING, urlEncodedSearchTerm);

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Geen verbinding");
                yield break;
            }

            ClearSearchResults();

            var jsonNode = JSON.Parse(webRequest.downloadHandler.text);
            var results = jsonNode["response"]["docs"];

            for (int i = 0; i < results.Count; i++)
            {
                var ID = results[i]["id"];
                var label = results[i]["weergavenaam"];

                GenerateResultItem(ID, label);
            }

            if(results.Count > 0)
                resultsParent.gameObject.SetActive(true);
        }

        private void ClearSearchResults()
        {
            foreach (Transform child in resultsParent.transform)
            {
                Destroy(child.gameObject);
            }
            resultsParent.gameObject.SetActive(false);
        }

        private void GenerateResultItem(string ID, string label)
        {
            Button buttonObject = (suggestionPrefab != null) ? Instantiate(suggestionPrefab) : GenerateResultButton(label);
            buttonObject.transform.SetParent(resultsParent.transform, false);
            TextMeshProUGUI textObject = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if(textObject == null) {
                Debug.Log("Make sure your suggestionPrefab ahs a TextMeshProUGUI component");
                return;
            }
            textObject.text = label;

            buttonObject.onClick.AddListener(delegate { GeoDataLookup(ID, label); });
        }

        private Button GenerateResultButton(string label)
        {
            GameObject suggestion = new GameObject { name = label };

            RectTransform rt = suggestion.AddComponent<RectTransform>();
            rt.SetParent(resultsParent.transform);
            rt.localScale = new Vector3(1, 1, 1);
            rt.sizeDelta = new Vector2(160, 10);

            suggestion.SetActive(true);

            TextMeshProUGUI textObject = suggestion.AddComponent<TextMeshProUGUI>();
            textObject.color = Color.black;
            textObject.fontSize = 10;
            textObject.text = label;
            textObject.margin = new Vector4(10, 10, 10, 10);

            Button buttonObject = suggestion.AddComponent<Button>();
            return buttonObject;
        }

        private void GeoDataLookup(string ID, string label)
        {
            searchInputField.SetTextWithoutNotify(label);
            StartCoroutine(GeoDataLookupRoutine(ID));
            ClearSearchResults();
        }

        IEnumerator GeoDataLookupRoutine(string ID)
        {
            string url = $"{locationLookupEndpoint}?id={ID}";
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Geen verbinding");
                yield break;
            }

            var jsonNode = JSON.Parse(webRequest.downloadHandler.text);
            var results = jsonNode["response"]["docs"];

            string centroid = results[0]["centroide_ll"];
            Debug.Log($"Centroid: {centroid}");
            string residentialObjectID = results[0]["adresseerbaarobject_id"];

            Vector3 targetLocation = ExtractUnityLocation(ref centroid);
            
            onCoordinateFound.Invoke(new Coordinate(targetLocation));

            if (moveCamera)
            {
                var targetPos = new Vector3(targetLocation.x, 300, targetLocation.z - 300);
      
                if(easeCamera){
                    StartCoroutine(LerpCamera(mainCamera.gameObject, targetPos, targetCameraRotation, 2));
                    yield return new WaitForSeconds(2);
                    StartCoroutine(GetBAGID(residentialObjectID));
                    yield break;
                }

                mainCamera.gameObject.transform.position = targetPos;
            }   
        }

        private static Vector3 ExtractUnityLocation(ref string locationData)
        {
            locationData = locationData.Replace("POINT(", "").Replace(")", "").Replace("\"", "");
            string[] lonLat = locationData.Split(' ');

            double.TryParse(lonLat[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon);
            double.TryParse(lonLat[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat);

            var wgs84 = new Coordinate(CoordinateSystem.WGS84_LatLon, lat, lon);
            var unityCoordinate = wgs84.ToUnity();
            
            return unityCoordinate;
        }

        IEnumerator LerpCamera(GameObject targetObj, Vector3 endPos, Quaternion endRot, float duration)
        {
            float t = 0;
            Vector3 startPos = targetObj.transform.position;
            Quaternion startRot = targetObj.transform.rotation;
            while (t < duration)
            {
                targetObj.transform.position = Vector3.Lerp(startPos, endPos, cameraMoveCurve.Evaluate(t / duration));
                targetObj.transform.rotation = Quaternion.Lerp(startRot, endRot, cameraMoveCurve.Evaluate(t / duration));
                t += Time.deltaTime;
                yield return null;
            }

            targetObj.transform.position = endPos;
        }

        IEnumerator GetBAGID(string residentialObjectID)
        {
            string url = $"{bagWfsEndpoint}?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature&typeName=bag:verblijfsobject&count=100&outputFormat=xml&srsName=EPSG:28992&filter=<Filter><PropertyIsEqualTo><PropertyName>identificatie</PropertyName><Literal>{residentialObjectID}</Literal></PropertyIsEqualTo></Filter>";

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Geen verbinding");
                yield break;
            }

            JSONNode jsonNode = JSON.Parse(webRequest.downloadHandler.text);
            JSONNode bagId = jsonNode["features"][0]["properties"]["pandidentificatie"];

#if UNITY_EDITOR
            Debug.Log($"BAG ID: {bagId}");
#endif

            List<string> bagIDs = new List<string> { bagId };

            onSelectedBuildings.Invoke(bagIDs);
        }
    }
}
