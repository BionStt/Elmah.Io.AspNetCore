﻿using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace Elmah.Io.AspNetCore.Mvc
{
    public class ElmahIoTagHelper : TagHelper
    {
        public ElmahIoTagHelper(IOptions<ElmahIoOptions> options)
        {
            Options = options.Value;
        }

        public ElmahIoOptions Options { get; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "script";
            output.Attributes.SetAttribute("src", $"https://cdn.rawgit.com/elmahio/elmah.io.js/3.0.0-beta1/dist/elmahio.min.js?apiKey={Options.ApiKey}&logId={Options.LogId}");
        }
    }
}
