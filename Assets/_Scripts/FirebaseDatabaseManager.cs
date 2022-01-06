using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseDatabaseManager : MonoBehaviour
{
    public static FirebaseDatabaseManager instance = null;
    private DatabaseReference databaseReference;

    string sessionID;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
        DontDestroyOnLoad(this.gameObject);
        Initialize();
    }

    private void Start()
    {
        sessionID = CreateRandomSessionID(10);
    }

    void Initialize()
    {
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://creativity-1589a.firebaseio.com/");
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference; FirebaseDatabase.DefaultInstance
         .GetReference("upload")
         .ValueChanged += HandleValueChanged;
    }

    void HandleValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        print(args.Snapshot.Value);
        if (args.Snapshot.Value.ToString() == "t")
        {
            BlockSpawner.instance.UploadCanvas();
        }
    }

    string CreateRandomSessionID(int length)
    {
        string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string id = "";

        for (int i = 0; i < length; i++)
        {
            id += letters[Random.Range(0, letters.Length - 1)];
        }

        return id;
    }

    public async Task<bool> AddNewCanvas(string canvasJson)
    {
        bool error = false;
        print("Adding Canvas JSON to database");
        DatabaseReference canvasReference = databaseReference.Child("canvases/" + sessionID);
        await canvasReference.SetRawJsonValueAsync(canvasJson).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError(task.Exception);
                //LoginUIManager.instance.EnableLoadingPanel(false);
                error = true;
            } else if (task.IsCompleted)
            {
                Debug.Log("Success");
            }
        });

        //Upload sessionID to list
        error = false;
        print("Adding session ID");
        DatabaseReference userInfoReference = databaseReference.Child("ids/");
        Dictionary<string, object> sessionIDUpdate = new Dictionary<string, object>();
        sessionIDUpdate[sessionID] = sessionID;

        await userInfoReference.UpdateChildrenAsync(sessionIDUpdate).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError(task.Exception);
                error = true;
            }
        });

        return !error;
    }
}
