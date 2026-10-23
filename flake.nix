{
  description = "Development environment for the MoreSailwindSails mod";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-26.05";

  outputs = { self, nixpkgs }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
    in
    {
      devShells.${system}.default = pkgs.mkShell {
        packages = [
          pkgs.blender
          pkgs.coreutils
          pkgs.dotnet-sdk_8
          pkgs.git
          pkgs.gh
          pkgs.ilspycmd
          pkgs.pre-commit
          pkgs.prettier
          pkgs.python3
          pkgs.uv
          pkgs.ruff
        ];
        # Native Python wheels and the AOT type-tree reader use these system libraries.
        LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [
          pkgs.stdenv.cc.cc.lib
          pkgs.zlib
          pkgs.icu
          pkgs.openssl
        ];
        # Keep Nix tool dependencies from shadowing the uv-locked Python packages.
        shellHook = ''
          unset PYTHONPATH
        '';
      };
    };
}
