using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadLevel : MonoBehaviour
{
	[SerializeField] private KeyCode reloadKey = KeyCode.None;

	private void Update()
	{
		if (Input.GetKeyDown(reloadKey))
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
	}
}
