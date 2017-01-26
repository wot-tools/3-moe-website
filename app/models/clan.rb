class Clan < ApplicationRecord
	has_many :players
	has_many :marks, through: :players
end
