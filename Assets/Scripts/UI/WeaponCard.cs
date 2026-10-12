using TMPro;
using UnholyTrinity.Weapons;
using UnityEngine;

public class WeaponCard : ActionCard
{
    [field: SerializeField] public TMP_Text AmmoText { get; private set; }
    
    public static WeaponCard Factory(Weapon weapon, Color? textColor)
    {
        WeaponData weaponData = weapon.Data;
        textColor ??= Color.black;
        WeaponCard weaponCard = (WeaponCard)ActionCard.Factory(
            weaponData.DisplayName, weaponData.TickCost,
            textColor);

        weaponCard.AmmoText = weaponCard.gameObject.AddComponent<TMP_Text>();
        weaponCard.AmmoText.alignment = TextAlignmentOptions.Center;
        weaponCard.AmmoText.enableAutoSizing = true;
        weaponCard.AmmoText.text = $"{weapon.Ammo}/{weaponData.MaxAmmo}";
        return weaponCard;
    }
    
    
}