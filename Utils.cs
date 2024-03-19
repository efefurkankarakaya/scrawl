public class Utils
{

  public const string initialScript = @"const defaultGetter = Object.getOwnPropertyDescriptor(
      Navigator.prototype,
      'webdriver'
    ).get;
    defaultGetter.apply(navigator);
    defaultGetter.toString();
    Object.defineProperty(Navigator.prototype, 'webdriver', {
      set: undefined,
      enumerable: true,
      configurable: true,
      get: new Proxy(defaultGetter, {
        apply: (target, thisArg, args) => {
          Reflect.apply(target, thisArg, args);
          return false;
        },
      }),
    });
    const patchedGetter = Object.getOwnPropertyDescriptor(
      Navigator.prototype,
      'webdriver'
    ).get;
    patchedGetter.apply(navigator);
    patchedGetter.toString();";
}