using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviour
{
    private bool isQuestionsLoaded = false;
    private bool isNsfwQuestionsLoaded = false;
    private bool isWouldYouRathersLoaded = false;
    private bool isAnonymousNamesLoaded = false;
    private bool isFriendshipTipsLoaded = false;

    private bool isWinterHolidaySeason = DateTime.Today.Month == 1 || DateTime.Today.Month == 12;
    private bool isHolidayQuestionsLoaded = false;
    private bool isHolidayWouldYouRathersLoaded = false;

    void Start()
    {
        // if webGL, this will be something like "http://..."
        string assetPath = Application.streamingAssetsPath;

        bool isWebGl = assetPath.Contains("://") ||
                         assetPath.Contains(":///");
        TextAssetsContainer.setIsWebGl(isWebGl);

        Debug.Log("isWebGl: " + isWebGl);

        try
        {
            if (isWebGl)
            {
                StartCoroutine(SendRequest());
            }
            else // desktop app
            {
                SceneManager.LoadScene("Personalitest");
            }
        }
        catch
        {
            // handle failure
        }
    }

    void Update()
    {
        bool seasonalContentReady = true;
        //Check seasonal questions
        if (isWinterHolidaySeason)
        {
            seasonalContentReady = isHolidayQuestionsLoaded && isHolidayWouldYouRathersLoaded;
        }

        // check to see if asset has been successfully read yet
        if (seasonalContentReady && isQuestionsLoaded && isNsfwQuestionsLoaded && isWouldYouRathersLoaded && isAnonymousNamesLoaded & isFriendshipTipsLoaded)
        {
            // once asset is successfully read, 
            // load the next screen (e.g. main menu or gameplay)
            SceneManager.LoadScene("Personalitest");
        } else
        {
            Debug.Log("isQuestionsLoaded: " + isQuestionsLoaded + "isNsfwQuestionsLoaded: " + isNsfwQuestionsLoaded + 
                " isWouldYouRathersLoaded: " + isWouldYouRathersLoaded + " isAnonymousNamesLoaded: " + isAnonymousNamesLoaded +
                " isFriendshipTipsLoaded" + isFriendshipTipsLoaded); 
        }

        // need to consider what happens if 
        // asset fails to be read for some reason
    }

    private IEnumerator SendRequest()
    {
        string assetPath = Application.streamingAssetsPath;

        if(isWinterHolidaySeason)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(Path.Combine(assetPath, "winterWouldYouRathers.txt")))
            {
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    // handle failure
                }
                else
                {
                    try
                    {
                        // entire file is returned via downloadHandler
                        string fileContents = request.downloadHandler.text;
                        Debug.Log("Loader holidayWouldYouRather: " + fileContents);
                        // or
                        //byte[] fileContents = request.downloadHandler.data;

                        // do whatever you need to do with the file contents
                        TextAssetsContainer.setRawHolidayWouldYouRathersText(fileContents);
                        isHolidayWouldYouRathersLoaded = true;
                    }
                    catch (Exception x)
                    {
                        Debug.Log("failed to load holiday would you rather");
                    }
                }
            }

            using (UnityWebRequest request = UnityWebRequest.Get(Path.Combine(assetPath, "winterQuestions.txt")))
            {
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    // handle failure
                }
                else
                {
                    try
                    {
                        // entire file is returned via downloadHandler
                        string fileContents = request.downloadHandler.text;
                        Debug.Log("Loader winterQuestions: " + fileContents);
                        // or
                        //byte[] fileContents = request.downloadHandler.data;

                        // do whatever you need to do with the file contents
                        TextAssetsContainer.setRawHolidayQuestionsText(fileContents);
                        isHolidayQuestionsLoaded = true;
                    }
                    catch (Exception x)
                    {
                        Debug.Log("failed to load holiday questions");
                    }
                }
            }
        }

        using (UnityWebRequest request = UnityWebRequest.Get(Path.Combine(assetPath, "questions.txt")))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                // handle failure
            }
            else
            {
                try
                {
                    // entire file is returned via downloadHandler
                    string fileContents = request.downloadHandler.text;
                    Debug.Log("Loader questions: " + fileContents);
                    // or
                    //byte[] fileContents = request.downloadHandler.data;

                    // do whatever you need to do with the file contents
                    TextAssetsContainer.setRawQuestionsText(fileContents);
                    isQuestionsLoaded = true;
                }
                catch (Exception x)
                {
                    Debug.Log("failed to load questions");
                }
            }
        }

        using (UnityWebRequest request = UnityWebRequest.Get(Path.Combine(assetPath, "nsfwQuestions.txt")))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                // handle failure
            }
            else
            {
                try
                {
                    // entire file is returned via downloadHandler
                    string fileContents = request.downloadHandler.text;
                    Debug.Log("Loader questions: " + fileContents);
                    // or
                    //byte[] fileContents = request.downloadHandler.data;

                    // do whatever you need to do with the file contents
                    TextAssetsContainer.setRawNsfwQuestionsText(fileContents);
                    isNsfwQuestionsLoaded = true;
                }
                catch (Exception x)
                {
                    Debug.Log("failed to load questions");
                }
            }
        }

        using (UnityWebRequest request = UnityWebRequest.Get(Path.Combine(assetPath, "wouldYouRathers.txt")))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                // handle failure
            }
            else
            {
                try
                {
                    // entire file is returned via downloadHandler
                    string fileContents = request.downloadHandler.text;
                    Debug.Log("Loader wouldYouRater: " + fileContents);

                    // or
                    //byte[] fileContents = request.downloadHandler.data;

                    // do whatever you need to do with the file contents
                    TextAssetsContainer.setRawWouldYouRatherText(fileContents);
                    isWouldYouRathersLoaded = true;
                }
                catch (Exception x)
                {
                    Debug.Log("failed to load wouldYouRathers");
                }
            }
        }

        using (UnityWebRequest request = UnityWebRequest.Get(Path.Combine(assetPath, "anonymousNames.txt")))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log("failed to load anonymousNames - Network");
            }
            else
            {
                try
                {
                    // entire file is returned via downloadHandler
                    string fileContents = request.downloadHandler.text;
                    Debug.Log("Loader anonymousNames: " + fileContents);

                    // or
                    //byte[] fileContents = request.downloadHandler.data;

                    // do whatever you need to do with the file contents
                    TextAssetsContainer.setRawAnonymousNameText(fileContents);
                    isAnonymousNamesLoaded = true;
                }
                catch (Exception x)
                {
                    Debug.Log("failed to load anonymousNames - parsing");
                }
            }
        }

        using (UnityWebRequest request = UnityWebRequest.Get(Path.Combine(assetPath, "friendshipTips.txt")))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log("failed to load friendshipTips - Network");
            }
            else
            {
                try
                {
                    // entire file is returned via downloadHandler
                    string fileContents = request.downloadHandler.text;
                    Debug.Log("Loader friendshipTips: " + fileContents);

                    // or
                    //byte[] fileContents = request.downloadHandler.data;

                    // do whatever you need to do with the file contents
                    TextAssetsContainer.setRawFriendshipTipsText(fileContents);
                    isFriendshipTipsLoaded = true;
                }
                catch (Exception x)
                {
                    Debug.Log("failed to load friendshipTips - parsing");
                }
            }
        }
    }


}
