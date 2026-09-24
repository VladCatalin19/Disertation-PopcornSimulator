using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowControls : MonoBehaviour
{
	[SerializeField] private GameObject controlsItem = null;
	[SerializeField] private KeyCode toggleKey = KeyCode.None;

	private void Update()
	{
		if (Input.GetKeyDown(toggleKey))
		{
			bool isActive = controlsItem.activeSelf;
			bool toggledActive = !isActive;

			controlsItem.SetActive(toggledActive);
		}
	}
}
