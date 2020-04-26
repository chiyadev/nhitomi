namespace nhitomi.Models
{
    /// <summary>
    /// Material rating follows Danbooru conventions: https://danbooru.donmai.us/wiki_pages/10920
    /// </summary>
    public enum MaterialRating
    {
        /// <summary>
        /// <list type="bullet">
        ///   <item><description>All art clearly lacking sexual content.</description></item>
        ///   <item><description>Innocent romance between couples (kissing, hugging, eye contact, etc.).</description></item>
        ///   <item><description>Tasteful swimsuits.</description></item>
        ///   <item><description>Tasteful lingerie.</description></item>
        ///   <item><description>Tasteful panty shots.</description></item>
        ///   <item><description>Discreet, clearly non-sexual nudity that do not fall under Questionable or Explicit.</description></item>
        /// </list>
        /// </summary>
        Safe = 0,

        /// <summary>
        /// <list type="bullet">
        ///   <item><description>Non-genital nudity, including exposed breasts, nipples, or areolae.</description></item>
        ///   <item><description>Non-blatantly exposed genitals, full frontal nudity without additional "action" or bodily fluids.</description></item>
        ///   <item><description>Erect nipples under clothing.</description></item>
        ///   <item><description>Cameltoes and wedgies.</description></item>
        ///   <item><description>Images in the middle of disrobing or torn clothes that are designed to be sexually suggestive in nature.</description></item>
        ///   <item><description>Sexually suggestive acts (sucking suggestively on hotdogs, etc.).</description></item>
        ///   <item><description>Erections under clothes.</description></item>
        ///   <item><description>Sex toys not being used, or hinted at or hidden under clothes.</description></item>
        ///   <item><description>Mild sexual contact (ear biting, breast grabbing, saliva-swapping, etc.).</description></item>
        ///   <item><description>Pubic hair (while the rest of the genitals remain hidden).</description></item>
        ///   <item><description>Sex acts implied and hinted at, but not shown.</description></item>
        ///   <item><description>Actual intercourse, if portrayed in a restrained and tasteful manner (this can be tricky and is necessarily a judgement call, but if it concentrates on the act and omits anatomical details, it's likely okay).</description></item>
        ///   <item><description>Implied or obscured bodily fluids (someone sitting on the toilet, somewhat wet panties, etc. Again, this is a judgement call).</description></item>
        ///   <item><description>Bondage, spanking and more general BDSM activities without object insertion or bodily fluids.</description></item>
        ///   <item><description>Other assorted erotica, as long as it doesn't cross the porn line. While rather hard to define precisely, the basic test is that of intent -- does it show just to show sex, or is it rather as a part of some other, normal human activity?</description></item>
        /// </list>
        /// </summary>
        Questionable = 1,

        /// <summary>
        /// <list type="bullet">
        ///   <item><description>Blatantly exposed genitals (of either gender, censored or uncensored). "Blatantly" means in a way designed to draw the attention to the fact, by means of viewing angle or perspective. This includes spread pussy / anus, erect penises, etc.</description></item>
        ///   <item><description>Openly and unambiguously portrayed sex acts (including intercourse, oral, fingering, handjobs, masturbation, object insertion, etc). This includes censored images, as the presence of censorship makes it unambiguously clear what is happening behind that censor.</description></item>
        ///   <item><description>Clearly visible sexual fluids (cum and pussy juice).</description></item>
        /// </list>
        /// </summary>
        Explicit = 2
    }
}