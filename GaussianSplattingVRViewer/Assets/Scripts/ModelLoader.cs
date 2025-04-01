using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class ModelLoader : MonoBehaviour
{
    public TextAsset defaultModel;
    public GaussianSplattingModel modelPrefab;
    public ModelLoaderItem UIListItemPrefab;
    public RectTransform UIListElement;
    public UnityEvent NoModelLoaded;
    public MeasureLine measureLine;
    public Button refreshButton;

    delegate void InstanciateModel(int idx, bool value);

    private GaussianModelContainer[] gaussianModels = new GaussianModelContainer[] {};

    //private string[] modelList = new string[] {};
    //private string[] spriteList = new string[] {};
    //private bool[] modelLock;
    private GaussianSplattingModel[] models;
    private string serverPath;

    async void Start()
    {
        await APIManager.GetServerPath().ContinueWith((task) =>
        {
            serverPath = task.Result.Replace("\"", "");
        });
        updateModels();

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(UpdateModelButtonPressed);
        }
    }

    public void UpdateModelButtonPressed()
    {
        foreach (Transform child in UIListElement)
        {
            Destroy(child.gameObject);
        }
        updateModels();
    }

    public void ToggleModel(int idx, bool value)
    {
        if (models[idx] == null)
        {
            models[idx] = Instantiate(modelPrefab);
            models[idx].modelFilePath = gaussianModels[idx].gaussianPaths[0];
            models[idx].GetComponent<XRGrabInteractable>().selectEntered.AddListener(measureLine.select);
            models[idx].GetComponent<XRGrabInteractable>().selectExited.AddListener(measureLine.select);
        }

        //We are in the menu so we start deactivated
        models[idx].GetComponent<Collider>().enabled = false;
        models[idx].gameObject.SetActive(value);
    }

    public void LockModel(int idx, bool value)
    {
        gaussianModels[idx].modelLock[0] = value;
        if (models[idx] != null && value)
        {
            models[idx].GetComponent<Collider>().enabled = false;
        }
    }

    public void DeactivateGrab()
    {
        foreach (GaussianSplattingModel model in models)
        {
            if (model != null)
            {
                //model.GetComponent<XRGrabInteractable>().enabled = false;
                model.GetComponent<Collider>().enabled = false;
            }
        }
    }

    public void ActivateGrab()
    {
        for (int i = 0; i < gaussianModels.Length; ++i)
        {
            if (models[i] != null)
            {
                models[i].GetComponent<Collider>().enabled = true && !gaussianModels[i].modelLock[0];
            }
        }
    }

    private async void updateModels()
    {
        await APIManager.GetGaussians().ContinueWith((task) =>
        {
            print("call serveur");
            Gaussian[] gaussians = task.Result;
            gaussianModels = new GaussianModelContainer[gaussians.Length];

            for (int i = 0; i < gaussians.Length; ++i){
                GaussianModelContainer model = new();
                print(gaussians[i].PlyDirectory);
                print(serverPath);
                print(gaussians[i].PlyDirectory.Replace(".", serverPath) + "/point_cloud");
                Dictionary<string, string> plyRecovery = genericPlyRecovery(gaussians[i].PlyDirectory.Replace(".", serverPath) + "/point_cloud");

                model.title = gaussians[i].Name;
                model.sprite = gaussians[i].Image;
                model.gaussianPaths = new(plyRecovery.Keys);

                model.gaussianIterations = new(plyRecovery.Values);
                model.modelLock = new();

                for (int j = 0; j < model.gaussianPaths.Count; j++)
                {
                    model.modelLock.Add(false);
                }
                gaussianModels[i] = model;
            }

        });
        createView();

       //await APIManager.GetGaussians().ContinueWith((task) => {
       //     print("call serveur");
       //     Gaussian[] gaussians = task.Result;
       //     modelList = new string[gaussians.Length];
       //     spriteList = new string[gaussians.Length];
       //     for(int i = 0; i < gaussians.Length; ++i){
       //        modelList[i] = gaussians[i].PlyDirectory + "/point_cloud/iteration_30000/point_cloud.ply";
       //        spriteList[i] = gaussians[i].Image;
       //     }
       
       // });
       // createView();
    }

    static Dictionary<string, string> genericPlyRecovery(string pointCloudFile)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();

        if (Directory.Exists(pointCloudFile))
        {
            
            string[] subFiles = Directory.GetDirectories(pointCloudFile);
            foreach (string subFile in subFiles)
            {
                string subFileName = subFile.Split('\\')[^1];
                print(subFileName);
                if (subFileName.StartsWith("iteration_"))
                {
                    string iteration = subFileName.Replace("iteration_", "");
                    string[] plyFiles = Directory.GetFiles(subFile, "*.ply");
                    result[iteration] = plyFiles[0];
                }
            }
        }
        return result;

    } 

    private void createView()
    {
        models = new GaussianSplattingModel[gaussianModels.Length];

        for (int i = 0; i < gaussianModels.Length; i++)
        {
            ModelLoaderItem listItem = Instantiate(UIListItemPrefab, UIListElement);
            listItem.index = i;
            listItem.loader = this;
            print(gaussianModels[i]);
            listItem.text.text = Path.GetFileName(gaussianModels[i].gaussianPaths[0]);
            listItem.image.sprite = LoadImageToUI(gaussianModels[i].sprite);
            listItem.title.text = gaussianModels[i].title;
        }
        NoModelLoaded.Invoke();
    }

    private Sprite LoadImageToUI(string path)
    {
        path = path.Replace("./", serverPath+"/");
        if (File.Exists(path))
        {
            byte[] imageData = File.ReadAllBytes(path);
            Texture2D texture = new(2, 2);
            if (texture.LoadImage(imageData))
            {
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                return sprite;
            }
            else
            {
                Debug.LogError("Failed to load image data.");
                return null;
            }
        }

        Debug.LogError("File not found at: " + path);
        return null;
    }
}

class GaussianModelContainer
{
    public string title { get; set; }
    public string sprite { get; set; }
    public List<string> gaussianPaths { get; set; }
    public List<string> gaussianIterations { get; set; }
    public List<bool> modelLock { get; set; }

}