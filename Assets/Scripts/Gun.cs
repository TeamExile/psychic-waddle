using UnityEngine;

namespace Friendslop
{
    /// <summary>
    /// Basic gun/weapon system for shooting mechanics.
    /// Can be extended for different weapon types, ammo, etc.
    /// </summary>
    public class Gun : MonoBehaviour
    {
        [Header("Gun Settings")]
        [SerializeField] private float fireRate = 0.5f; // Time between shots
        [SerializeField] private float range = 100f;
        [SerializeField] private int damage = 10;
        
        [Header("Visual Effects")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject bulletHitEffectPrefab;
        
        [Header("Audio")]
        [SerializeField] private AudioClip gunShotSound;
        
        private float _nextFireTime = 0f;
        private AudioSource _audioSource;
        
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Create muzzle point if it doesn't exist
            if (muzzlePoint == null)
            {
                GameObject muzzleObj = new GameObject("MuzzlePoint");
                muzzleObj.transform.SetParent(transform);
                muzzleObj.transform.localPosition = new Vector3(0, 0, 1f);
                muzzlePoint = muzzleObj.transform;
            }
        }
        
        /// <summary>
        /// Attempts to fire the gun. Returns true if shot was fired.
        /// </summary>
        public bool TryShoot()
        {
            if (Time.time < _nextFireTime)
            {
                return false;
            }
            
            _nextFireTime = Time.time + fireRate;
            PerformShoot();
            return true;
        }
        
        private void PerformShoot()
        {
            // Play sound effect
            if (gunShotSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(gunShotSound);
            }
            
            // Spawn muzzle flash
            if (muzzleFlashPrefab != null && muzzlePoint != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
                Destroy(flash, 0.1f);
            }
            
            // Perform raycast to detect hits
            Ray ray = new Ray(muzzlePoint.position, muzzlePoint.forward);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, range))
            {
                Debug.Log($"Hit {hit.collider.gameObject.name}");
                
                // Spawn hit effect
                if (bulletHitEffectPrefab != null)
                {
                    GameObject hitEffect = Instantiate(bulletHitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(hitEffect, 1f);
                }
                
                // Deal damage if the hit object has a health component
                // This can be extended later with a health system
            }
            
            // Visual feedback in editor
            Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 1f);
        }
        
        // Public properties for network synchronization
        public float FireRate => fireRate;
        public float Range => range;
        public int Damage => damage;
    }
}
