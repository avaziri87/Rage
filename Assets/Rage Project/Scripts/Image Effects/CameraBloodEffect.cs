using UnityEngine;
using System.Collections;

[ExecuteInEditMode()]
public class CameraBloodEffect : MonoBehaviour 
{
	[SerializeField] Shader 		_shader			= null;
	[SerializeField] float			_bloodAmount	= 0.0f;
	[SerializeField] float			_minBloodAmount = 0.0f;
	[SerializeField] Texture2D		_bloodTexture	= null;
	[SerializeField] Texture2D		_bloodNormalMap = null;
	[SerializeField] float			_distortion		= 1.0f;
	[SerializeField] bool			_autoFade		= true;
	[SerializeField] float			_fadeSpeed		= 0.05f;

	private			Material		_material		= null;

	public float bloodAmount	{ get { return _bloodAmount; }		set { _bloodAmount = value; } }
	public float minBloodAmount { get { return _minBloodAmount; }	set { _minBloodAmount = value; } }
	public float fadeSpeed		{ get { return _fadeSpeed; }		set { _fadeSpeed = value; } }
	public bool autoFade		{ get { return _autoFade; }			set { _autoFade = value; } }

	private void Update()
	{
		if(_autoFade)
		{
			_bloodAmount -= _fadeSpeed * Time.deltaTime;
			_bloodAmount = Mathf.Max(_bloodAmount, _minBloodAmount);
		}
	}
	void OnRenderImage( RenderTexture src, RenderTexture dest )
	{
		if (_shader==null) return;
		if (_material==null)
		{
			_material = new Material( _shader );
		}

		if (_material==null) return;

		//data sent to shader
		if (_bloodTexture != null) _material.SetTexture("_bloodTex",_bloodTexture);
		if (_bloodNormalMap != null) _material.SetTexture("_bloodBump", _bloodNormalMap);
		_material.SetFloat("_distortion", _distortion);
		_material.SetFloat("_bloodAmount", _bloodAmount);

		Graphics.Blit( src, dest, _material);
	}

}
