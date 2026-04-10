using UnityEngine;

namespace Bindings.Sample
{
    public sealed class CountModel
    {
        public int Count { get; set; }
    }

// simple
    [ViewModel]
    public partial class CountViewModel1
    {
        [Required]
        private readonly CountModel _model;

        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text)]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Increment()
        {
            _count += 1;
            _publisher.PublishRebindMessage<CountViewModel1>();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            _count -= 1;
            _publisher.PublishRebindMessage<CountViewModel1>();
        }

        partial void OnPostBind()
        {
            _model.Count = _count;
        }
    }

// requireBindImplementation
    [ViewModel(requireBindImplementation: true)]
    public partial class CountViewModel2
    {
        [Required]
        private readonly CountModel _model;

        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text)]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Increment()
        {
            _count += 1;
            _publisher.PublishRebindMessage<CountViewModel2>();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            _count -= 1;
            _publisher.PublishRebindMessage<CountViewModel2>();
        }

        partial void OnPostBind()
        {
            _model.Count = _count;
        }
    }

// already SerializableAttribute
    [ViewModel]
    [System.Serializable]
    public partial class CountViewModel3
    {
        [Required]
        private readonly CountModel _model;

        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text)]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Increment()
        {
            _count += 1;
            PublishRebindMessage();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            _count -= 1;
            PublishRebindMessage();
        }

        partial void OnPostBind()
        {
            _model.Count = _count;
        }
    }

// non model
    [ViewModel]
    public partial class CountViewModel4
    {
        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text)]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Increment()
        {
            _count += 1;
            PublishRebindMessage();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            _count -= 1;
            PublishRebindMessage();
        }
    }

// multi models
    [ViewModel]
    public partial class CountViewModel5
    {
        [Required]
        private readonly CountModel _model;

        [Required]
        private readonly CountModel _model2;

        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text)]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Increment()
        {
            _count += 1;
            PublishRebindMessage();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            _count -= 1;
            PublishRebindMessage();
        }

        partial void OnPostBind()
        {
            _model.Count = _count;
            _model2.Count = _count;
        }
    }

// same id pair
    [ViewModel]
    public partial class CountViewModel6
    {
        [Required]
        private readonly CountModel _model;

        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text)]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick, id: 1)]
        public void Increment()
        {
            _count += 1;
            PublishRebindMessage();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick, id: 1)]
        public void Decrement()
        {
            _count -= 1;
            PublishRebindMessage();
        }

        partial void OnPostBind()
        {
            _model.Count = _count;
        }
    }

// use format and use non text field
    [ViewModel]
    public partial class CountViewModel7
    {
        [Required]
        private readonly CountModel _model;

        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text, format: "N0")]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Toggle.interactable)]
        private bool _interactable;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Increment()
        {
            _count += 1;
            PublishRebindMessage();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            _count -= 1;
            PublishRebindMessage();
        }

        partial void OnPostBind()
        {
            _model.Count = _count;
            _interactable = true;
        }
    }
}